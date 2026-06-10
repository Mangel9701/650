using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Studio650.ColorField
{
    [Serializable]
    public class Vector3ColorEvent : UnityEvent<Vector3> { }

    [Serializable]
    public class ColorEvent : UnityEvent<Color> { }

    [Preserve]
    public class BaseColorFieldEditorUI : MonoBehaviour
    {
        private const int TextureSize = 128;
        private const float ByteMax = 255f;

        [Header("Campo de color")]
        [SerializeField] private RawImage saturationValueField;
        [SerializeField] private RectTransform saturationValueHandle;
        [SerializeField] private RawImage hueField;
        [SerializeField] private RectTransform hueHandle;
        [SerializeField] private RawImage alphaField;
        [SerializeField] private RectTransform alphaHandle;
        [SerializeField] private Image previewColor;
        [SerializeField] private Image previousColor;

        [Header("Canales RGB")]
        [SerializeField] private TMP_InputField redInput;
        [SerializeField] private TMP_InputField greenInput;
        [SerializeField] private TMP_InputField blueInput;

        [Header("Canales HSV")]
        [SerializeField] private TMP_InputField hueInput;
        [SerializeField] private TMP_InputField saturationInput;
        [SerializeField] private TMP_InputField valueInput;

        [Header("Alpha y Hex")]
        [SerializeField] private TMP_InputField alphaInput;
        [SerializeField] private TMP_InputField hexInput;

        [Header("Comportamiento")]
        [SerializeField] private bool manageGameplayCursor = true;
        [SerializeField] private Color initialColor = Color.white;
        [SerializeField] private Button closeButton;

        [Header("Responsive")]
        [SerializeField] private bool responsiveLayout = true;
        [SerializeField] private Vector2 referenceSize = new Vector2(420f, 500f);
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private Vector2 referenceContentSize = new Vector2(384f, 464f);
        [SerializeField] private Vector2 viewportPadding = new Vector2(24f, 24f);
        [SerializeField] private float minResponsiveScale = 0.65f;
        [SerializeField] private float maxResponsiveScale = 1.2f;

        [Header("Eventos")]
        public Vector3ColorEvent onRgb255Changed = new Vector3ColorEvent();
        public ColorEvent onColorChanged = new ColorEvent();

        private Texture2D saturationValueTexture;
        private Texture2D hueTexture;
        private Texture2D alphaTexture;
        private Color currentColor = Color.white;
        private Color originalColor = Color.white;
        private float hue;
        private float saturation;
        private float value = 1f;
        private UIManager uiManager;
        private bool suppressCallbacks;
        private bool gameplayCursorEnabled;
        private RectTransform rectTransform;

        public Vector3 Rgb255 => ToRgb255(currentColor);
        public Color CurrentColor => currentColor;

        private void Awake()
        {
            currentColor = initialColor;
            originalColor = initialColor;
            Color.RGBToHSV(currentColor, out hue, out saturation, out value);
            rectTransform = GetComponent<RectTransform>();
            AutoWireMissingReferences();
            BuildTextures();
            BindInputCallbacks();
            BindCloseButton();
            UpdateResponsiveLayout();
            RefreshUi(false);
        }

        private void OnEnable()
        {
            if (manageGameplayCursor)
            {
                gameplayCursorEnabled = true;
                EnableUiCursor();
            }
        }

        private void LateUpdate()
        {
            UpdateResponsiveLayout();

            if (manageGameplayCursor && (Cursor.lockState != CursorLockMode.None || !Cursor.visible))
                EnableUiCursor();
        }

        private void OnDisable()
        {
            if (manageGameplayCursor)
                RestoreGameplayCursor();
        }

        public void SetColor(Color color)
        {
            SetColorInternal(color, true);
        }

        public void SetRgb255(Vector3 rgb255)
        {
            SetColorInternal(new Color(
                Mathf.Clamp01(rgb255.x / ByteMax),
                Mathf.Clamp01(rgb255.y / ByteMax),
                Mathf.Clamp01(rgb255.z / ByteMax),
                currentColor.a), true);
        }

        public void SetOriginalColor(Color color)
        {
            originalColor = color;
            if (previousColor != null)
                previousColor.color = originalColor;
        }

        public void ClosePanel()
        {
            if (manageGameplayCursor)
                RestoreGameplayCursor();

            gameObject.SetActive(false);
        }

        public void SelectSaturationValue(Vector2 normalizedPosition)
        {
            saturation = Mathf.Clamp01(normalizedPosition.x);
            value = Mathf.Clamp01(normalizedPosition.y);
            SetColorFromHsv();
        }

        public void SelectHue(float normalizedHue)
        {
            hue = Mathf.Clamp01(normalizedHue);
            SetColorFromHsv();
        }

        public void SelectAlpha(float normalizedAlpha)
        {
            Color next = currentColor;
            next.a = Mathf.Clamp01(normalizedAlpha);
            SetColorInternal(next, true);
        }

        private void SetColorFromHsv()
        {
            Color next = Color.HSVToRGB(hue, saturation, value);
            next.a = currentColor.a;
            SetColorInternal(next, true, false);
        }

        private void SetColorInternal(Color color, bool notify, bool recalculateHsv = true)
        {
            currentColor = ClampColor(color);
            if (recalculateHsv)
            {
                Color.RGBToHSV(currentColor, out hue, out saturation, out value);
            }

            RefreshUi(notify);
        }

        private void RefreshUi(bool notify)
        {
            suppressCallbacks = true;

            UpdateTextureAssignments();
            UpdateSaturationValueTexture();
            UpdateAlphaTexture();
            UpdateHandles();
            UpdatePreview();
            UpdateInputs();

            suppressCallbacks = false;

            if (!notify)
                return;

            onColorChanged.Invoke(currentColor);
            onRgb255Changed.Invoke(ToRgb255(currentColor));
        }

        private void BuildTextures()
        {
            saturationValueTexture = CreateReadableTexture(TextureSize, TextureSize, "Runtime SV Field");
            hueTexture = CreateReadableTexture(1, TextureSize, "Runtime Hue Field");
            alphaTexture = CreateReadableTexture(TextureSize, 1, "Runtime Alpha Field");

            for (int y = 0; y < TextureSize; y++)
            {
                float normalizedHue = y / (TextureSize - 1f);
                hueTexture.SetPixel(0, y, Color.HSVToRGB(normalizedHue, 1f, 1f));
            }

            hueTexture.Apply(false, false);
            UpdateSaturationValueTexture();
            UpdateAlphaTexture();
        }

        private void UpdateTextureAssignments()
        {
            if (saturationValueField != null)
                saturationValueField.texture = saturationValueTexture;

            if (hueField != null)
                hueField.texture = hueTexture;

            if (alphaField != null)
                alphaField.texture = alphaTexture;
        }

        private void UpdateSaturationValueTexture()
        {
            if (saturationValueTexture == null)
                return;

            for (int y = 0; y < TextureSize; y++)
            {
                float textureValue = y / (TextureSize - 1f);
                for (int x = 0; x < TextureSize; x++)
                {
                    float textureSaturation = x / (TextureSize - 1f);
                    saturationValueTexture.SetPixel(x, y, Color.HSVToRGB(hue, textureSaturation, textureValue));
                }
            }

            saturationValueTexture.Apply(false, false);
        }

        private void UpdateAlphaTexture()
        {
            if (alphaTexture == null)
                return;

            Color opaque = currentColor;
            opaque.a = 1f;
            Color transparent = opaque;
            transparent.a = 0f;

            for (int x = 0; x < TextureSize; x++)
            {
                float t = x / (TextureSize - 1f);
                alphaTexture.SetPixel(x, 0, Color.Lerp(transparent, opaque, t));
            }

            alphaTexture.Apply(false, false);
        }

        private void UpdateHandles()
        {
            PositionHandleInRect(saturationValueHandle, saturationValueField, saturation, value);
            PositionHandleInRect(hueHandle, hueField, 0.5f, hue);
            PositionHandleInRect(alphaHandle, alphaField, currentColor.a, 0.5f);
        }

        private void UpdatePreview()
        {
            if (previewColor != null)
                previewColor.color = currentColor;

            if (previousColor != null)
                previousColor.color = originalColor;
        }

        private void UpdateInputs()
        {
            SetInputText(redInput, ToByte(currentColor.r).ToString(CultureInfo.InvariantCulture));
            SetInputText(greenInput, ToByte(currentColor.g).ToString(CultureInfo.InvariantCulture));
            SetInputText(blueInput, ToByte(currentColor.b).ToString(CultureInfo.InvariantCulture));
            SetInputText(alphaInput, ToByte(currentColor.a).ToString(CultureInfo.InvariantCulture));
            SetInputText(hueInput, Mathf.RoundToInt(hue * 360f).ToString(CultureInfo.InvariantCulture));
            SetInputText(saturationInput, Mathf.RoundToInt(saturation * 100f).ToString(CultureInfo.InvariantCulture));
            SetInputText(valueInput, Mathf.RoundToInt(value * 100f).ToString(CultureInfo.InvariantCulture));
            SetInputText(hexInput, ColorUtility.ToHtmlStringRGBA(currentColor));
        }

        private void BindInputCallbacks()
        {
            AddEndEdit(redInput, _ => ApplyRgbInputs());
            AddEndEdit(greenInput, _ => ApplyRgbInputs());
            AddEndEdit(blueInput, _ => ApplyRgbInputs());
            AddEndEdit(alphaInput, _ => ApplyRgbInputs());
            AddEndEdit(hueInput, _ => ApplyHsvInputs());
            AddEndEdit(saturationInput, _ => ApplyHsvInputs());
            AddEndEdit(valueInput, _ => ApplyHsvInputs());
            AddEndEdit(hexInput, ApplyHexInput);
        }

        private void ApplyRgbInputs()
        {
            if (suppressCallbacks)
                return;

            SetColorInternal(new Color(
                ReadByte01(redInput, currentColor.r),
                ReadByte01(greenInput, currentColor.g),
                ReadByte01(blueInput, currentColor.b),
                ReadByte01(alphaInput, currentColor.a)), true);
        }

        private void ApplyHsvInputs()
        {
            if (suppressCallbacks)
                return;

            hue = Mathf.Clamp(ReadFloat(hueInput, hue * 360f), 0f, 360f) / 360f;
            saturation = Mathf.Clamp01(ReadFloat(saturationInput, saturation * 100f) / 100f);
            value = Mathf.Clamp01(ReadFloat(valueInput, value * 100f) / 100f);
            SetColorFromHsv();
        }

        private void ApplyHexInput(string text)
        {
            if (suppressCallbacks || string.IsNullOrWhiteSpace(text))
                return;

            string normalized = text.Trim();
            if (!normalized.StartsWith("#", StringComparison.Ordinal))
                normalized = "#" + normalized;

            if (ColorUtility.TryParseHtmlString(normalized, out Color parsed))
            {
                if (normalized.Length <= 7)
                    parsed.a = currentColor.a;

                SetColorInternal(parsed, true);
            }
            else
            {
                UpdateInputs();
            }
        }

        private void EnableUiCursor()
        {
            ResolveUiManager();
            if (uiManager != null)
            {
                uiManager.showCursor();
                return;
            }

            BenignoGLWebBridge.SetGameplayPointerMode(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void RestoreGameplayCursor()
        {
            if (!gameplayCursorEnabled)
                return;

            gameplayCursorEnabled = false;
            ResolveUiManager();
            if (uiManager != null)
            {
                uiManager.hideCursor();
                return;
            }

            BenignoGLWebBridge.SetGameplayPointerMode(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void AutoWireMissingReferences()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (contentRoot == null)
            {
                Transform content = FindChildByName(transform, "Content");
                if (content != null)
                    contentRoot = content as RectTransform;
            }

            if (closeButton == null)
            {
                Transform closeTransform = FindChildByName(transform, "Button_Volver");
                if (closeTransform != null)
                    closeButton = closeTransform.GetComponent<Button>();
            }
        }

        private void BindCloseButton()
        {
            if (closeButton == null)
                return;

            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            foreach (Transform child in root)
            {
                if (child.name == childName)
                    return child;

                Transform found = FindChildByName(child, childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void ResolveUiManager()
        {
            if (uiManager == null)
                uiManager = FindAnyObjectByType<UIManager>();
        }

        private void UpdateResponsiveLayout()
        {
            if (!responsiveLayout)
                return;

            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (contentRoot == null)
            {
                Transform content = FindChildByName(transform, "Content");
                if (content != null)
                    contentRoot = content as RectTransform;
            }

            if (rectTransform == null || contentRoot == null || referenceContentSize.x <= 0f || referenceContentSize.y <= 0f)
                return;

            Rect availableRect = rectTransform.rect;
            Vector2 availableSize = availableRect.size - viewportPadding * 2f;

            if (availableSize.x <= 0f || availableSize.y <= 0f)
                return;

            float widthScale = availableSize.x / referenceContentSize.x;
            float heightScale = availableSize.y / referenceContentSize.y;
            float scale = Mathf.Clamp(Mathf.Min(widthScale, heightScale), minResponsiveScale, maxResponsiveScale);

            contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
            contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
            contentRoot.pivot = new Vector2(0.5f, 0.5f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = referenceContentSize;
            contentRoot.localScale = Vector3.one * scale;
        }

        private static Texture2D CreateReadableTexture(int width, int height, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            return texture;
        }

        private static void PositionHandleInRect(RectTransform handle, Graphic field, float x, float y)
        {
            if (handle == null || field == null || field.transform is not RectTransform fieldRect)
                return;

            Rect rect = fieldRect.rect;
            Vector2 fieldLocalPosition = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, Mathf.Clamp01(x)),
                Mathf.Lerp(rect.yMin, rect.yMax, Mathf.Clamp01(y)));

            RectTransform targetParent = handle.parent as RectTransform;
            if (targetParent == null)
                return;

            Vector3 worldPosition = fieldRect.TransformPoint(fieldLocalPosition);
            Vector3 parentLocalPosition = targetParent.InverseTransformPoint(worldPosition);
            handle.anchoredPosition = parentLocalPosition;
        }

        private static Color ClampColor(Color color)
        {
            return new Color(
                Mathf.Clamp01(color.r),
                Mathf.Clamp01(color.g),
                Mathf.Clamp01(color.b),
                Mathf.Clamp01(color.a));
        }

        private static int ToByte(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * ByteMax);
        }

        private static Vector3 ToRgb255(Color color)
        {
            return new Vector3(ToByte(color.r), ToByte(color.g), ToByte(color.b));
        }

        private static float ReadByte01(TMP_InputField input, float fallback)
        {
            return Mathf.Clamp(ReadFloat(input, fallback * ByteMax), 0f, ByteMax) / ByteMax;
        }

        private static float ReadFloat(TMP_InputField input, float fallback)
        {
            if (input == null)
                return fallback;

            return float.TryParse(input.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                ? parsed
                : fallback;
        }

        private static void AddEndEdit(TMP_InputField input, UnityAction<string> callback)
        {
            if (input != null)
                input.onEndEdit.AddListener(callback);
        }

        private static void SetInputText(TMP_InputField input, string value)
        {
            if (input != null)
                input.SetTextWithoutNotify(value);
        }
    }

}
