using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// PBR Material Validator - Unity 6.3 / URP
///
/// Canales validados:
///   BASE COLOR (sin textura) → HSV: V [10%–80%], S ≤ 90%
///   BASE COLOR (con textura) → fuerza RGB a (255,255,255)
///   SMOOTHNESS (sin textura en canal roughness/smoothness) → rango [0.2–0.7]
///
/// Correcciones separadas por canal + botón "Corregir Ambos".
/// Filtro de shaders configurable (por defecto: "Lit").
/// </summary>
public class PBRMaterialValidator : EditorWindow
{
    // ── Constantes PBR ───────────────────────────────────────────────────────
    private const float V_MIN   = 0.10f;    // 10 %
    private const float V_MAX   = 0.80f;    // 80 %
    private const float S_MAX   = 0.90f;    // 90 %
    private const float SM_MIN  = 0.20f;    // smoothness mínimo
    private const float SM_MAX  = 0.70f;    // smoothness máximo

    /// <summary>
    /// Margen de tolerancia para comparaciones de punto flotante.
    /// Evita que un valor de 0.09999847 (guardado como 0.10) se marque como error.
    /// Solo valores CLARAMENTE fuera del rango (> EPSILON más allá del límite)
    /// se consideran problemáticos.
    /// </summary>
    private const float EPSILON = 0.0005f;

    // ── Propiedades de color base ─────────────────────────────────────────────
    private static readonly string[] BASE_COLOR_PROPS =
    {
        "_BaseColor",       // URP Lit / Unlit
        "_Color",           // Built-in Legacy
        "_MainColor",
    };

    // ── Propiedades de textura de base map ───────────────────────────────────
    private static readonly string[] BASE_TEX_PROPS =
    {
        "_BaseMap",
        "_MainTex",
        "_BaseColorMap",
        "_Albedo",
    };

    // ── Propiedades de valor de smoothness/glossiness ─────────────────────────
    private static readonly string[] SMOOTHNESS_PROPS =
    {
        "_Smoothness",      // URP Lit (Unity 2021+, workflow Metallic)
        "_GlossMapScale",   // URP Lit cuando hay textura como fuente de smoothness
        "_Glossiness",      // Built-in Standard
        "_Roughness",       // custom shaders (workflow invertido: sm = 1 - roughness)
    };

    // ── Propiedades de textura que alimentan el canal de smoothness/roughness ──
    private static readonly string[] SMOOTHNESS_TEX_PROPS =
    {
        "_MetallicGlossMap",    // URP Lit: canal A de la textura metallic
        "_SpecGlossMap",        // URP Lit specular workflow
        "_RoughnessMap",
        "_SmoothnessMap",
    };

    // ── Keywords de shader por defecto ───────────────────────────────────────
    private static readonly string[] DEFAULT_SHADER_KEYWORDS = { "Lit" };

    // ── Estado ───────────────────────────────────────────────────────────────
    private List<MaterialReport> _reports              = new List<MaterialReport>();
    private Vector2              _scroll;
    private bool                 _scanned              = false;
    private bool                 _showOnlyProblematic  = true;
    private int                  _fixedColorCount      = 0;
    private int                  _fixedSmoothnessCount = 0;

    // ── Shader filter ─────────────────────────────────────────────────────────
    private List<string> _shaderKeywords  = new List<string>(DEFAULT_SHADER_KEYWORDS);
    private string       _newKeywordInput = "";
    private bool         _showShaderFilter = true;

    // ── Estilos ───────────────────────────────────────────────────────────────
    private GUIStyle _headerStyle, _subHeaderStyle;
    private GUIStyle _okStyle, _warnStyle, _infoStyle;
    private GUIStyle _btnColorStyle, _btnSmStyle, _btnBothStyle;
    private bool     _stylesInitialized = false;

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/PBR Material Validator (URP)")]
    public static void ShowWindow()
    {
        var win = GetWindow<PBRMaterialValidator>("PBR Validator");
        win.minSize = new Vector2(570, 490);
    }

    // ── Inicializar estilos ───────────────────────────────────────────────────
    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            normal   = { textColor = new Color(0.9f, 0.75f, 0.2f) }
        };
        _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal   = { textColor = new Color(0.7f, 0.85f, 1f) }
        };
        _okStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = new Color(0.4f, 0.9f, 0.4f) }
        };
        _warnStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = new Color(1f, 0.6f, 0.2f) }
        };
        _infoStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };
        _btnColorStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fontSize = 10,
            normal   = { textColor = new Color(1f, 0.85f, 0.3f) }
        };
        _btnSmStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fontSize = 10,
            normal   = { textColor = new Color(0.4f, 0.85f, 1f) }
        };
        _btnBothStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fontSize   = 10,
            fontStyle  = FontStyle.Bold,
            normal     = { textColor = new Color(0.6f, 1f, 0.6f) }
        };

        _stylesInitialized = true;
    }

    // ── GUI Principal ─────────────────────────────────────────────────────────
    private void OnGUI()
    {
        InitStyles();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("⬡  PBR Material Validator — URP", _headerStyle);
        EditorGUILayout.LabelField("Valida Base Color y Smoothness según rangos PBR físicamente correctos.", _infoStyle);
        EditorGUILayout.Space(4);

        // Reglas resumidas
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Reglas PBR:", _subHeaderStyle);
            EditorGUILayout.LabelField("  BASE COLOR — sin textura:  S ≤ 90%  |  V entre 10% y 80%  (límites inclusivos)", _infoStyle);
            EditorGUILayout.LabelField("  BASE COLOR — con textura:  color base debe ser RGB(255,255,255)", _infoStyle);
            EditorGUILayout.LabelField("  SMOOTHNESS — sin textura en canal sm/roughness:  rango [0.20 – 0.70]", _infoStyle);
        }

        EditorGUILayout.Space(6);

        // ── Shader filter ─────────────────────────────────────────────────────
        _showShaderFilter = EditorGUILayout.Foldout(_showShaderFilter,
            "🔎  Filtro de Shaders (keywords en nombre del shader)", true);

        if (_showShaderFilter)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "Se analizan materiales cuyo shader contenga alguna de estas palabras:", _infoStyle);

                for (int i = 0; i < _shaderKeywords.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"  • {_shaderKeywords[i]}", GUILayout.ExpandWidth(true));
                        if (GUILayout.Button("✕", GUILayout.Width(24)))
                        {
                            _shaderKeywords.RemoveAt(i);
                            _scanned = false;
                            break;
                        }
                    }
                }

                EditorGUILayout.Space(2);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _newKeywordInput = EditorGUILayout.TextField(_newKeywordInput);
                    if (GUILayout.Button("+ Agregar", GUILayout.Width(80)))
                    {
                        string kw = _newKeywordInput.Trim();
                        if (!string.IsNullOrEmpty(kw) && !_shaderKeywords.Contains(kw))
                        {
                            _shaderKeywords.Add(kw);
                            _scanned = false;
                        }
                        _newKeywordInput = "";
                        GUI.FocusControl(null);
                    }
                }

                if (_shaderKeywords.Count == 0)
                    EditorGUILayout.HelpBox("Sin keywords activas: se analizarán TODOS los materiales.", MessageType.Warning);
            }
        }

        EditorGUILayout.Space(6);

        // ── Botones globales ──────────────────────────────────────────────────
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("🔍  Escanear Escena", GUILayout.Height(30)))
                ScanScene();

            bool anyColor = _scanned && _reports.Exists(r => r.NeedsColorCorrection);
            bool anySm    = _scanned && _reports.Exists(r => r.NeedsSmoothnessCorrection);

            GUI.enabled = anyColor;
            if (GUILayout.Button("🎨  Corregir Color", GUILayout.Height(30)))
                FixAllColor();

            GUI.enabled = anySm;
            if (GUILayout.Button("✨  Corregir Smoothness", GUILayout.Height(30)))
                FixAllSmoothness();

            GUI.enabled = anyColor || anySm;
            if (GUILayout.Button("🔧  Corregir Ambos", GUILayout.Height(30)))
                FixAllBoth();

            GUI.enabled = true;
        }

        EditorGUILayout.Space(4);
        _showOnlyProblematic = EditorGUILayout.Toggle("Mostrar solo materiales con problemas", _showOnlyProblematic);
        EditorGUILayout.Space(4);

        // ── Resultados ────────────────────────────────────────────────────────
        if (_scanned)
        {
            int colorProb = _reports.FindAll(r => r.NeedsColorCorrection).Count;
            int smProb    = _reports.FindAll(r => r.NeedsSmoothnessCorrection).Count;
            EditorGUILayout.LabelField(
                $"Analizados: {_reports.Count}   |   ⚠ Color: {colorProb}   ⚠ Smoothness: {smProb}" +
                $"   |   Corregidos → color: {_fixedColorCount}  sm: {_fixedSmoothnessCount}",
                _infoStyle);

            EditorGUILayout.Space(4);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var rep in _reports)
            {
                bool anyProblem = rep.NeedsColorCorrection || rep.NeedsSmoothnessCorrection;
                if (_showOnlyProblematic && !anyProblem) continue;
                DrawReport(rep);
            }

            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("Haz clic en 'Escanear Escena' para analizar los materiales.", MessageType.Info);
        }
    }

    // ── Dibujar reporte de un material ───────────────────────────────────────
    private void DrawReport(MaterialReport rep)
    {
        bool anyProblem = rep.NeedsColorCorrection || rep.NeedsSmoothnessCorrection;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            // ── Fila de cabecera con botones ──────────────────────────────────
            using (new EditorGUILayout.HorizontalScope())
            {
                string icon       = anyProblem ? "⚠" : "✔";
                GUIStyle lblStyle = anyProblem ? _warnStyle : _okStyle;
                EditorGUILayout.LabelField($"{icon}  {rep.MaterialName}", lblStyle, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Seleccionar", GUILayout.Width(80)))
                    Selection.activeObject = rep.Material;

                GUI.enabled = rep.NeedsColorCorrection;
                if (GUILayout.Button("Color", _btnColorStyle, GUILayout.Width(46)))
                {
                    FixMaterialColor(rep);
                    _fixedColorCount++;
                    EditorUtility.SetDirty(rep.Material);
                }

                GUI.enabled = rep.NeedsSmoothnessCorrection;
                if (GUILayout.Button("Smooth", _btnSmStyle, GUILayout.Width(52)))
                {
                    FixMaterialSmoothness(rep);
                    _fixedSmoothnessCount++;
                    EditorUtility.SetDirty(rep.Material);
                }

                GUI.enabled = rep.NeedsColorCorrection || rep.NeedsSmoothnessCorrection;
                if (GUILayout.Button("Ambos", _btnBothStyle, GUILayout.Width(46)))
                {
                    if (rep.NeedsColorCorrection)      { FixMaterialColor(rep);      _fixedColorCount++; }
                    if (rep.NeedsSmoothnessCorrection) { FixMaterialSmoothness(rep); _fixedSmoothnessCount++; }
                    EditorUtility.SetDirty(rep.Material);
                }

                GUI.enabled = true;
            }

            // ── Detalle BASE COLOR ────────────────────────────────────────────
            if (rep.HasNoTexture)
            {
                string mark = rep.NeedsColorCorrection ? "⚠" : "✔";
                GUIStyle cs = rep.NeedsColorCorrection ? _warnStyle : _infoStyle;
                EditorGUILayout.LabelField(
                    $"   {mark} Color  H:{rep.OriginalH * 360f:F1}°  " +
                    $"S:{rep.OriginalS * 100f:F2}%  V:{rep.OriginalV * 100f:F2}%", cs);

                if (rep.NeedsColorCorrection)
                {
                    float corrS = Mathf.Clamp(rep.OriginalS, 0f, S_MAX);
                    float corrV = Mathf.Clamp(rep.OriginalV, V_MIN, V_MAX);
                    EditorGUILayout.LabelField(
                        $"          → corregido  S:{corrS * 100f:F1}%  V:{corrV * 100f:F1}%", _warnStyle);

                    var issues = new List<string>();
                    if (rep.OriginalV < V_MIN - EPSILON) issues.Add($"V muy oscuro ({rep.OriginalV * 100f:F3}% < 10%)");
                    if (rep.OriginalV > V_MAX + EPSILON) issues.Add($"V muy brillante ({rep.OriginalV * 100f:F3}% > 80%)");
                    if (rep.OriginalS > S_MAX + EPSILON) issues.Add($"S muy saturado ({rep.OriginalS * 100f:F3}% > 90%)");
                    EditorGUILayout.LabelField($"          Problemas: {string.Join(" | ", issues)}", _warnStyle);
                }
            }
            else
            {
                string mark = rep.NeedsColorCorrection ? "⚠" : "✔";
                GUIStyle cs = rep.NeedsColorCorrection ? _warnStyle : _infoStyle;
                EditorGUILayout.LabelField(
                    $"   {mark} Color (con textura)  " +
                    $"RGB({rep.TextureColorR}, {rep.TextureColorG}, {rep.TextureColorB})" +
                    (rep.NeedsColorCorrection ? " → debe ser (255,255,255)" : " ✔"), cs);
            }

            // ── Detalle SMOOTHNESS ─────────────────────────────────────────────
            if (!rep.HasSmoothnessTexture)
            {
                string mark       = rep.NeedsSmoothnessCorrection ? "⚠" : "✔";
                GUIStyle ss       = rep.NeedsSmoothnessCorrection ? _warnStyle : _infoStyle;
                string  propLabel = rep.IsRoughnessWorkflow ? "Roughness(inv→sm)" : "Smoothness";

                EditorGUILayout.LabelField(
                    $"   {mark} {propLabel}: {rep.OriginalSmoothness:F4}" +
                    (rep.NeedsSmoothnessCorrection
                        ? $"  → corregido: {Mathf.Clamp(rep.OriginalSmoothness, SM_MIN, SM_MAX):F4}  (rango [0.20–0.70])"
                        : "  ✔ dentro del rango [0.20–0.70]"),
                    ss);
            }
            else
            {
                EditorGUILayout.LabelField(
                    "   ✔ Smoothness: tiene textura en canal roughness/smoothness — no se modifica.", _infoStyle);
            }
        }
    }

    // ── Filtro de shader ──────────────────────────────────────────────────────
    private bool ShaderPassesFilter(Material mat)
    {
        if (_shaderKeywords.Count == 0) return true;
        if (mat.shader == null) return false;
        string name = mat.shader.name;
        foreach (var kw in _shaderKeywords)
            if (name.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        return false;
    }

    // ── Escaneo de escena ─────────────────────────────────────────────────────
    private void ScanScene()
    {
        _reports.Clear();
        _fixedColorCount      = 0;
        _fixedSmoothnessCount = 0;

        var renderers          = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var processedMaterials = new HashSet<int>();

        foreach (var rend in renderers)
        {
            foreach (var mat in rend.sharedMaterials)
            {
                if (mat == null) continue;
                int id = mat.GetInstanceID();
                if (processedMaterials.Contains(id)) continue;
                processedMaterials.Add(id);
                if (!ShaderPassesFilter(mat)) continue;
                _reports.Add(AnalyzeMaterial(mat));
            }
        }

        // Primero los que tienen algún problema
        _reports.Sort((a, b) =>
        {
            bool ap = a.NeedsColorCorrection || a.NeedsSmoothnessCorrection;
            bool bp = b.NeedsColorCorrection || b.NeedsSmoothnessCorrection;
            return bp.CompareTo(ap);
        });

        _scanned = true;
        Repaint();
    }

    // ── Análisis de un material ───────────────────────────────────────────────
    private MaterialReport AnalyzeMaterial(Material mat)
    {
        var rep = new MaterialReport { Material = mat, MaterialName = mat.name };

        // ── Base Color ────────────────────────────────────────────────────────
        bool hasBaseTex = false;
        foreach (var prop in BASE_TEX_PROPS)
        {
            if (mat.HasProperty(prop) && mat.GetTexture(prop) != null)
            { hasBaseTex = true; break; }
        }
        rep.HasNoTexture = !hasBaseTex;

        Color baseColor = Color.white;
        foreach (var prop in BASE_COLOR_PROPS)
        {
            if (mat.HasProperty(prop)) { baseColor = mat.GetColor(prop); break; }
        }

        if (hasBaseTex)
        {
            rep.TextureColorR = Mathf.RoundToInt(baseColor.r * 255f);
            rep.TextureColorG = Mathf.RoundToInt(baseColor.g * 255f);
            rep.TextureColorB = Mathf.RoundToInt(baseColor.b * 255f);
            rep.NeedsColorCorrection =
                rep.TextureColorR != 255 || rep.TextureColorG != 255 || rep.TextureColorB != 255;
        }
        else
        {
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            rep.OriginalH = h;
            rep.OriginalS = s;
            rep.OriginalV = v;

            // EPSILON evita falsos positivos por ruido de punto flotante.
            // Un valor de 0.09999847 (resultado de guardar/leer 0.10) NO es un error.
            rep.NeedsColorCorrection =
                v < (V_MIN - EPSILON) ||   // claramente por debajo de 10 %
                v > (V_MAX + EPSILON) ||   // claramente por encima de 80 %
                s > (S_MAX + EPSILON);     // claramente por encima de 90 %
        }

        // ── Smoothness ────────────────────────────────────────────────────────
        bool hasSmTex = false;
        foreach (var prop in SMOOTHNESS_TEX_PROPS)
        {
            if (mat.HasProperty(prop) && mat.GetTexture(prop) != null)
            { hasSmTex = true; break; }
        }
        rep.HasSmoothnessTexture = hasSmTex;

        if (!hasSmTex)
        {
            float smValue    = -1f;
            bool  isRoughness = false;

            foreach (var prop in SMOOTHNESS_PROPS)
            {
                if (!mat.HasProperty(prop)) continue;
                if (prop == "_Roughness")
                {
                    smValue    = 1f - mat.GetFloat(prop);
                    isRoughness = true;
                }
                else
                {
                    smValue = mat.GetFloat(prop);
                }
                break;
            }

            rep.IsRoughnessWorkflow = isRoughness;
            rep.OriginalSmoothness  = smValue >= 0f ? smValue : 0.5f;

            if (smValue >= 0f)
            {
                rep.NeedsSmoothnessCorrection =
                    smValue < (SM_MIN - EPSILON) ||
                    smValue > (SM_MAX + EPSILON);
            }
        }

        return rep;
    }

    // ── Corregir COLOR ────────────────────────────────────────────────────────
    private void FixMaterialColor(MaterialReport rep)
    {
        if (rep.Material == null || !rep.NeedsColorCorrection) return;

        Undo.RecordObject(rep.Material, "PBR Fix Base Color");

        Color oldColor = Color.white;
        foreach (var prop in BASE_COLOR_PROPS)
        {
            if (rep.Material.HasProperty(prop)) { oldColor = rep.Material.GetColor(prop); break; }
        }

        Color newColor;
        if (!rep.HasNoTexture)
        {
            newColor = new Color(1f, 1f, 1f, oldColor.a);
            rep.TextureColorR = rep.TextureColorG = rep.TextureColorB = 255;
        }
        else
        {
            float s = Mathf.Clamp(rep.OriginalS, 0f, S_MAX);
            float v = Mathf.Clamp(rep.OriginalV, V_MIN, V_MAX);
            newColor   = Color.HSVToRGB(rep.OriginalH, s, v);
            newColor.a = oldColor.a;
            rep.OriginalS = s;
            rep.OriginalV = v;
        }

        foreach (var prop in BASE_COLOR_PROPS)
        {
            if (rep.Material.HasProperty(prop))
                rep.Material.SetColor(prop, newColor);
        }

        rep.NeedsColorCorrection = false;
    }

    // ── Corregir SMOOTHNESS ───────────────────────────────────────────────────
    private void FixMaterialSmoothness(MaterialReport rep)
    {
        if (rep.Material == null || !rep.NeedsSmoothnessCorrection) return;

        Undo.RecordObject(rep.Material, "PBR Fix Smoothness");

        float corrected = Mathf.Clamp(rep.OriginalSmoothness, SM_MIN, SM_MAX);

        foreach (var prop in SMOOTHNESS_PROPS)
        {
            if (!rep.Material.HasProperty(prop)) continue;
            float valueToSet = prop == "_Roughness" ? (1f - corrected) : corrected;
            rep.Material.SetFloat(prop, valueToSet);
            break;
        }

        rep.OriginalSmoothness        = corrected;
        rep.NeedsSmoothnessCorrection = false;
    }

    // ── Corregir todos: color ─────────────────────────────────────────────────
    private void FixAllColor()
    {
        int count = 0;
        foreach (var rep in _reports)
        {
            if (!rep.NeedsColorCorrection) continue;
            FixMaterialColor(rep);
            EditorUtility.SetDirty(rep.Material);
            count++;
        }
        _fixedColorCount += count;
        AssetDatabase.SaveAssets();
        Debug.Log($"[PBR Validator] Color corregido en {count} materiales.");
        Repaint();
    }

    // ── Corregir todos: smoothness ────────────────────────────────────────────
    private void FixAllSmoothness()
    {
        int count = 0;
        foreach (var rep in _reports)
        {
            if (!rep.NeedsSmoothnessCorrection) continue;
            FixMaterialSmoothness(rep);
            EditorUtility.SetDirty(rep.Material);
            count++;
        }
        _fixedSmoothnessCount += count;
        AssetDatabase.SaveAssets();
        Debug.Log($"[PBR Validator] Smoothness corregido en {count} materiales.");
        Repaint();
    }

    // ── Corregir todos: ambos ─────────────────────────────────────────────────
    private void FixAllBoth()
    {
        int cCount = 0, sCount = 0;
        foreach (var rep in _reports)
        {
            bool dirty = false;
            if (rep.NeedsColorCorrection)      { FixMaterialColor(rep);      cCount++; dirty = true; }
            if (rep.NeedsSmoothnessCorrection) { FixMaterialSmoothness(rep); sCount++; dirty = true; }
            if (dirty) EditorUtility.SetDirty(rep.Material);
        }
        _fixedColorCount      += cCount;
        _fixedSmoothnessCount += sCount;
        AssetDatabase.SaveAssets();
        Debug.Log($"[PBR Validator] Corregidos → color: {cCount}  smoothness: {sCount}");
        Repaint();
    }

    // ── Estructura de datos ───────────────────────────────────────────────────
    private class MaterialReport
    {
        public Material Material;
        public string   MaterialName;

        // BASE COLOR
        public bool  HasNoTexture;
        public float OriginalH, OriginalS, OriginalV;           // sin textura
        public int   TextureColorR, TextureColorG, TextureColorB; // con textura
        public bool  NeedsColorCorrection;

        // SMOOTHNESS
        public bool  HasSmoothnessTexture;
        public float OriginalSmoothness;
        public bool  IsRoughnessWorkflow;
        public bool  NeedsSmoothnessCorrection;
    }
}
