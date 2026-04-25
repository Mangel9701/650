using UnityEditor;
using UnityEngine;

public class InteractionConfigManagerWindow : EditorWindow
{
    private InteractionConfigSO config;
    private Vector2 scroll;

    private string newInteractionAssetName = "NewInteractionItem";
    private DefaultAsset targetFolder;
    private InteractionItemSO existingInteractionToAdd;

    [MenuItem("Tools/Interactions/Open Manager")]
    public static void OpenWindow()
    {
        GetWindow<InteractionConfigManagerWindow>("Interaction Manager");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Configuración general", EditorStyles.boldLabel);

        config = (InteractionConfigSO)EditorGUILayout.ObjectField(
            "Config",
            config,
            typeof(InteractionConfigSO),
            false
        );

        if (config == null)
        {
            EditorGUILayout.HelpBox("Asigna un InteractionConfigSO.", MessageType.Info);

            if (GUILayout.Button("Crear nuevo InteractionConfigSO"))
                InteractionConfigCreator.CreateConfig();

            return;
        }

        SerializedObject configSO = new SerializedObject(config);
        SerializedProperty mediaBaseUrlProp = configSO.FindProperty("mediaBaseUrl");
        SerializedProperty interactionsProp = configSO.FindProperty("interactions");

        EditorGUILayout.PropertyField(mediaBaseUrlProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Crear interacción nueva", EditorStyles.boldLabel);

        newInteractionAssetName = EditorGUILayout.TextField("Nombre asset", newInteractionAssetName);
        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Carpeta destino", targetFolder, typeof(DefaultAsset), false);

        if (GUILayout.Button("Crear y agregar interacción"))
            CreateAndAddInteraction();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Agregar interacción existente", EditorStyles.boldLabel);

        existingInteractionToAdd = (InteractionItemSO)EditorGUILayout.ObjectField(
            "Interaction existente",
            existingInteractionToAdd,
            typeof(InteractionItemSO),
            false
        );

        if (GUILayout.Button("Agregar existente al config"))
            AddExistingInteraction();

        EditorGUILayout.Space();

        if (GUILayout.Button("Exportar Config a JSON"))
            InteractionConfigJsonExporter.ExportConfigToJson(config);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Interacciones", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < interactionsProp.arraySize; i++)
        {
            SerializedProperty element = interactionsProp.GetArrayElementAtIndex(i);
            InteractionItemSO item = element.objectReferenceValue as InteractionItemSO;

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Elemento {i}", EditorStyles.boldLabel);

            if (GUILayout.Button("Seleccionar", GUILayout.Width(85)) && item != null)
            {
                Selection.activeObject = item;
                EditorGUIUtility.PingObject(item);
            }

            if (GUILayout.Button("Subir", GUILayout.Width(60)) && i > 0)
                interactionsProp.MoveArrayElement(i, i - 1);

            if (GUILayout.Button("Bajar", GUILayout.Width(60)) && i < interactionsProp.arraySize - 1)
                interactionsProp.MoveArrayElement(i, i + 1);

            if (GUILayout.Button("Quitar", GUILayout.Width(70)))
            {
                interactionsProp.DeleteArrayElementAtIndex(i);
                break;
            }

            EditorGUILayout.EndHorizontal();

            if (item == null)
            {
                EditorGUILayout.HelpBox("Referencia nula.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                continue;
            }

            DrawInteractionInlineEditor(item, config);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();

        configSO.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(config);
    }

    private void CreateAndAddInteraction()
    {
        if (config == null)
            return;

        string folderPath = "Assets";

        if (targetFolder != null)
        {
            string selectedPath = AssetDatabase.GetAssetPath(targetFolder);
            if (AssetDatabase.IsValidFolder(selectedPath))
                folderPath = selectedPath;
        }

        string safeName = string.IsNullOrWhiteSpace(newInteractionAssetName)
            ? "NewInteractionItem"
            : newInteractionAssetName.Trim();

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{safeName}.asset");

        InteractionItemSO newItem = ScriptableObject.CreateInstance<InteractionItemSO>();
        newItem.nombre = safeName;
        newItem.descripcion = "";
        newItem.mediaType = InteractionMediaType.Image;
        newItem.remoteMediaName = safeName;
        newItem.extensionMode = InteractionMediaExtensionMode.Png;
        newItem.customExtension = "png";
        newItem.NormalizeMediaFields();

        AssetDatabase.CreateAsset(newItem, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Undo.RecordObject(config, "Agregar InteractionItem");
        config.interactions.Add(newItem);

        EditorUtility.SetDirty(newItem);
        EditorUtility.SetDirty(config);

        Selection.activeObject = newItem;
        EditorGUIUtility.PingObject(newItem);
    }

    private void AddExistingInteraction()
    {
        if (config == null || existingInteractionToAdd == null)
            return;

        if (!config.interactions.Contains(existingInteractionToAdd))
        {
            Undo.RecordObject(config, "Agregar Interaction existente");
            config.interactions.Add(existingInteractionToAdd);
            EditorUtility.SetDirty(config);
        }
    }

    private void DrawInteractionInlineEditor(InteractionItemSO item, InteractionConfigSO currentConfig)
    {
        SerializedObject itemSO = new SerializedObject(item);

        SerializedProperty nombreProp = itemSO.FindProperty("nombre");
        SerializedProperty descripcionProp = itemSO.FindProperty("descripcion");
        SerializedProperty mediaTypeProp = itemSO.FindProperty("mediaType");
        SerializedProperty remoteMediaNameProp = itemSO.FindProperty("remoteMediaName");
        SerializedProperty extensionModeProp = itemSO.FindProperty("extensionMode");
        SerializedProperty customExtensionProp = itemSO.FindProperty("customExtension");

        SerializedProperty showSlideOnlyProp = itemSO.FindProperty("showSlideOnly");

        SerializedProperty oscilateProp = itemSO.FindProperty("oscilate");
        SerializedProperty videoPositionProp = itemSO.FindProperty("videoPosition");
        SerializedProperty videoScaleProp = itemSO.FindProperty("videoScale");

        SerializedProperty uiPositionProp = itemSO.FindProperty("uiPosition");
        SerializedProperty interactivePointPositionProp = itemSO.FindProperty("interactivePointPosition");

        EditorGUILayout.ObjectField("Asset", item, typeof(InteractionItemSO), false);

        EditorGUILayout.PropertyField(nombreProp, new GUIContent("Nombre"));
        EditorGUILayout.PropertyField(descripcionProp, new GUIContent("Descripción"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Media", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(mediaTypeProp, new GUIContent("Tipo media"));
        EditorGUILayout.PropertyField(remoteMediaNameProp, new GUIContent("Nombre media"));
        EditorGUILayout.PropertyField(extensionModeProp, new GUIContent("Extensión"));

        InteractionMediaExtensionMode mode = (InteractionMediaExtensionMode)extensionModeProp.enumValueIndex;

        if (mode == InteractionMediaExtensionMode.Custom)
            EditorGUILayout.PropertyField(customExtensionProp, new GUIContent("Extensión custom"));

        InteractionMediaType mediaType = (InteractionMediaType)mediaTypeProp.enumValueIndex;

        EditorGUILayout.Space();

        if (mediaType == InteractionMediaType.Image)
        {
            EditorGUILayout.LabelField("Opciones imagen", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showSlideOnlyProp, new GUIContent("Solo slide"));
        }
        else if (mediaType == InteractionMediaType.Video)
        {
            EditorGUILayout.LabelField("Opciones video", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(oscilateProp, new GUIContent("Oscilar"));
            EditorGUILayout.PropertyField(videoPositionProp, new GUIContent("Posición video"));
            EditorGUILayout.PropertyField(videoScaleProp, new GUIContent("Escala video"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Posiciones UI", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(uiPositionProp, new GUIContent("Ubicación UI"));
        EditorGUILayout.PropertyField(interactivePointPositionProp, new GUIContent("Punto interactivo"));

        itemSO.ApplyModifiedProperties();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Vista previa", EditorStyles.boldLabel);

        string sanitizedBaseName = item.GetSanitizedMediaBaseName();
        string finalFileName = item.GetFinalMediaFileName();
        string finalUrl = item.GetFullMediaUrl(currentConfig != null ? currentConfig.mediaBaseUrl : string.Empty);

        EditorGUILayout.LabelField("Nombre sanitizado", string.IsNullOrEmpty(sanitizedBaseName) ? "-" : sanitizedBaseName);
        EditorGUILayout.LabelField("Archivo final", string.IsNullOrEmpty(finalFileName) ? "-" : finalFileName);
        EditorGUILayout.LabelField("URL final", string.IsNullOrEmpty(finalUrl) ? "-" : finalUrl);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Normalizar media"))
        {
            Undo.RecordObject(item, "Normalizar media interacción");
            item.NormalizeMediaFields();
            EditorUtility.SetDirty(item);
        }

        if (GUILayout.Button("Abrir asset"))
        {
            Selection.activeObject = item;
            EditorGUIUtility.PingObject(item);
        }

        EditorGUILayout.EndHorizontal();

        if (GUI.changed)
            EditorUtility.SetDirty(item);
    }
}