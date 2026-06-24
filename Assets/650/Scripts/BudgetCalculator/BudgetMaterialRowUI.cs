using TMPro;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Studio650.Budget
{
    [Preserve]
    public class BudgetMaterialRowUI : MonoBehaviour
    {
        private const float PreferredHeight = 62f;

        [SerializeField] private Image previewImage;
        [SerializeField] private RawImage previewTextureImage;
        [SerializeField] private TMP_Text componentText;
        [SerializeField] private TMP_Text materialText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text priceText;

        public void SetData(string component, string material, string description, string price, Material previewMaterial)
        {
            SetData(component, material, description, price, previewMaterial, false, Color.white);
        }

        public void SetData(
            string component,
            string material,
            string description,
            string price,
            Material previewMaterial,
            bool hasTintOverride,
            Color tintOverride)
        {
            AutoWireMissingReferences();
            ApplyLayout();

            if (componentText != null)
                componentText.text = component;

            if (materialText != null)
                materialText.text = material;

            if (descriptionText != null)
                descriptionText.text = description;

            if (priceText != null)
                priceText.text = price;

            ApplyPreview(previewMaterial, hasTintOverride, tintOverride);
        }

        private void Awake()
        {
            AutoWireMissingReferences();
            ApplyLayout();
        }

        private void AutoWireMissingReferences()
        {
            if (previewImage != null && componentText != null && materialText != null && descriptionText != null && priceText != null)
                return;

            foreach (var image in GetComponentsInChildren<Image>(true))
            {
                if (image.name.Contains("IcoMaterial"))
                {
                    previewImage = image;
                    break;
                }
            }

            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name.Contains("Componente"))
                    componentText = text;
                else if (text.name.Contains("Material"))
                    materialText = text;
                else if (text.name.Contains("Descripcion") || text.name.Contains("Descripción"))
                    descriptionText = text;
                else if (text.name.Contains("Precio"))
                    priceText = text;
            }
        }

        private void ApplyLayout()
        {
            var rowRect = transform as RectTransform;
            if (rowRect != null)
            {
                rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, PreferredHeight);
            }

            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = gameObject.AddComponent<LayoutElement>();

            layoutElement.minHeight = PreferredHeight;
            layoutElement.preferredHeight = PreferredHeight;
            layoutElement.flexibleHeight = 0f;

            ApplyPreviewLayout();
            ApplyTextLayout(componentText, -5f, 15f, 12f, FontStyles.Bold, TextOverflowModes.Ellipsis);
            ApplyTextLayout(materialText, -19f, 15f, 11f, FontStyles.Normal, TextOverflowModes.Ellipsis);
            ApplyTextLayout(descriptionText, -34f, 26f, 9.5f, FontStyles.Normal, TextOverflowModes.Ellipsis);
            ApplyPriceLayout();
        }

        private void ApplyPreviewLayout()
        {
            if (previewImage == null)
                return;

            var rect = previewImage.rectTransform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(31f, 0f);
            rect.sizeDelta = new Vector2(42f, 42f);
            previewImage.preserveAspect = true;

            EnsurePreviewMask();

            if (previewTextureImage != null)
            {
                var textureRect = previewTextureImage.rectTransform;
                textureRect.anchorMin = Vector2.zero;
                textureRect.anchorMax = Vector2.one;
                textureRect.pivot = new Vector2(0.5f, 0.5f);
                textureRect.anchoredPosition = Vector2.zero;
                textureRect.sizeDelta = Vector2.zero;
                previewTextureImage.raycastTarget = false;
            }
        }

        private void ApplyTextLayout(TMP_Text text, float y, float height, float fontSize, FontStyles style, TextOverflowModes overflowMode)
        {
            if (text == null)
                return;

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(63f, y - height);
            rect.offsetMax = new Vector2(-84f, y);
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = overflowMode;
            text.alignment = TextAlignmentOptions.Left;
        }

        private void ApplyPriceLayout()
        {
            if (priceText == null)
                return;

            var rect = priceText.rectTransform;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-6f, 0f);
            rect.sizeDelta = new Vector2(78f, 20f);
            priceText.fontSize = 11f;
            priceText.textWrappingMode = TextWrappingModes.NoWrap;
            priceText.overflowMode = TextOverflowModes.Ellipsis;
            priceText.alignment = TextAlignmentOptions.Right;
        }

        private void ApplyPreview(Material material, bool hasTintOverride, Color tintOverride)
        {
            if (previewImage == null)
                return;

            Texture texture = ResolveMaterialTexture(material);
            Color tint = hasTintOverride ? tintOverride : ResolveMaterialTint(material);
            if (texture != null)
            {
                EnsurePreviewMask();
                previewImage.color = Color.white;

                if (previewTextureImage != null)
                {
                    previewTextureImage.gameObject.SetActive(true);
                    previewTextureImage.texture = texture;
                    previewTextureImage.color = tint;
                }

                return;
            }

            if (previewTextureImage != null)
            {
                previewTextureImage.texture = null;
                previewTextureImage.gameObject.SetActive(false);
            }

            previewImage.color = tint;
        }

        private void EnsurePreviewMask()
        {
            if (previewImage == null)
                return;

            var mask = previewImage.GetComponent<Mask>();
            if (mask == null)
                mask = previewImage.gameObject.AddComponent<Mask>();

            mask.showMaskGraphic = true;

            if (previewTextureImage == null)
            {
                Transform existing = previewImage.transform.Find("PreviewTexture");
                if (existing != null)
                    previewTextureImage = existing.GetComponent<RawImage>();
            }

            if (previewTextureImage == null)
            {
                var textureObject = new GameObject("PreviewTexture", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                textureObject.transform.SetParent(previewImage.transform, false);
                previewTextureImage = textureObject.GetComponent<RawImage>();
            }
        }

        private static Color ResolveMaterialTint(Material material)
        {
            if (material == null)
                return new Color(0.78f, 0.78f, 0.78f, 1f);

            if (material.HasColor("_BaseColor"))
                return material.GetColor("_BaseColor");

            if (material.HasColor("_Color"))
                return material.GetColor("_Color");

            return new Color(0.78f, 0.78f, 0.78f, 1f);
        }

        private static Texture ResolveMaterialTexture(Material material)
        {
            if (material == null)
                return null;

            Texture texture = GetTextureIfAssigned(material, "_BaseColor");
            if (texture != null)
                return texture;

            texture = GetTextureIfAssigned(material, "_BaseMap");
            if (texture != null)
                return texture;

            texture = GetTextureIfAssigned(material, "_MainTex");
            if (texture != null)
                return texture;

            return material.mainTexture;
        }

        private static Texture GetTextureIfAssigned(Material material, string propertyName)
        {
            if (!material.HasTexture(propertyName))
                return null;

            return material.GetTexture(propertyName);
        }
    }
}
