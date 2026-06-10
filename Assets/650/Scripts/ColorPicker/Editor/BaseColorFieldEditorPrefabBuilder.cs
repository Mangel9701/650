using Studio650.ColorField;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Studio650.ColorField.Editor
{
    public static class BaseColorFieldEditorPrefabBuilder
    {
        private const string PrefabPath = "Assets/650/Prefabs/Panel_BaseColorField.prefab";

        [InitializeOnLoadMethod]
        private static void BuildMissingPrefabAfterReload()
        {
            if (Application.isBatchMode || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                    BuildPrefab();
            };
        }

        [MenuItem("650/UI/Build Base Color Field Editor Prefab")]
        public static void BuildPrefab()
        {
            GameObject root = CreatePanelRoot();
            BaseColorFieldEditorUI editor = root.AddComponent<BaseColorFieldEditorUI>();
            BaseColorMaterialApplier applier = root.AddComponent<BaseColorMaterialApplier>();

            RectTransform content = CreateRect("Content", root.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));
            content.offsetMin = new Vector2(18f, 18f);
            content.offsetMax = new Vector2(-18f, -18f);

            TMP_Text title = CreateLabel("Text (TMP)_Titulo", content, "Base Color", 18f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -32f), new Vector2(0f, 0f));

            Button closeButton = CreateButton("Button_Volver", content, "Volver");
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-90f, -30f), new Vector2(0f, 0f));

            RawImage saturationValueField = CreateRawImage("Field_SaturationValue", content, Color.white);
            SetRect(saturationValueField.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -250f), new Vector2(250f, -50f));
            saturationValueField.gameObject.AddComponent<BaseColorFieldEditorDragTarget>().Initialize(editor, BaseColorFieldEditorDragTarget.TargetKind.SaturationValue);

            RectTransform saturationValueHandle = CreateHandle("Handle_SaturationValue", saturationValueField.transform, 14f, Color.white);

            RawImage hueField = CreateRawImage("Field_Hue", content, Color.white);
            SetRect(hueField.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(262f, -250f), new Vector2(282f, -50f));
            hueField.gameObject.AddComponent<BaseColorFieldEditorDragTarget>().Initialize(editor, BaseColorFieldEditorDragTarget.TargetKind.Hue);

            RectTransform hueHandle = CreateHandle("Handle_Hue", hueField.transform, 18f, Color.black);
            hueHandle.sizeDelta = new Vector2(26f, 5f);

            Image previousColor = CreateImage("Preview_Previous", content, new Color(0.82f, 0.82f, 0.82f, 1f));
            SetRect(previousColor.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, -92f), new Vector2(382f, -50f));

            Image previewColor = CreateImage("Preview_Current", content, Color.white);
            SetRect(previewColor.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, -142f), new Vector2(382f, -100f));

            RawImage alphaField = CreateRawImage("Field_Alpha", content, Color.white);
            SetRect(alphaField.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, -186f), new Vector2(382f, -164f));
            alphaField.gameObject.AddComponent<BaseColorFieldEditorDragTarget>().Initialize(editor, BaseColorFieldEditorDragTarget.TargetKind.Alpha);

            RectTransform alphaHandle = CreateHandle("Handle_Alpha", alphaField.transform, 14f, Color.black);
            alphaHandle.sizeDelta = new Vector2(5f, 28f);

            TMP_InputField redInput = CreateLabeledInput(content, "R", "Input_R", new Vector2(0f, -300f));
            TMP_InputField greenInput = CreateLabeledInput(content, "G", "Input_G", new Vector2(128f, -300f));
            TMP_InputField blueInput = CreateLabeledInput(content, "B", "Input_B", new Vector2(256f, -300f));

            TMP_InputField hueInput = CreateLabeledInput(content, "H", "Input_H", new Vector2(0f, -350f));
            TMP_InputField saturationInput = CreateLabeledInput(content, "S", "Input_S", new Vector2(128f, -350f));
            TMP_InputField valueInput = CreateLabeledInput(content, "V", "Input_V", new Vector2(256f, -350f));

            TMP_InputField alphaInput = CreateLabeledInput(content, "A", "Input_A", new Vector2(0f, -400f));
            TMP_InputField hexInput = CreateLabeledInput(content, "Hex", "Input_Hex", new Vector2(128f, -400f), 254f);

            WireEditor(editor, saturationValueField, saturationValueHandle, hueField, hueHandle, alphaField, alphaHandle,
                previewColor, previousColor, redInput, greenInput, blueInput, hueInput, saturationInput, valueInput, alphaInput, hexInput, closeButton);

            UnityEventTools.AddPersistentListener(editor.onRgb255Changed, applier.ApplyRgb255);
            UnityEventTools.AddPersistentListener(closeButton.onClick, editor.ClosePanel);

            AssetDatabase.DeleteAsset(PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
            Object.DestroyImmediate(root);

            if (success)
                Debug.Log($"[BaseColorFieldEditorPrefabBuilder] Prefab creado en {PrefabPath}");
            else
                Debug.LogError($"[BaseColorFieldEditorPrefabBuilder] No se pudo crear el prefab en {PrefabPath}");
        }

        private static GameObject CreatePanelRoot()
        {
            var root = new GameObject("Panel_BaseColorField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(420f, 500f);

            Image background = root.GetComponent<Image>();
            background.color = new Color(0.08f, 0.09f, 0.1f, 0.96f);
            return root;
        }

        private static void WireEditor(
            BaseColorFieldEditorUI editor,
            RawImage saturationValueField,
            RectTransform saturationValueHandle,
            RawImage hueField,
            RectTransform hueHandle,
            RawImage alphaField,
            RectTransform alphaHandle,
            Image previewColor,
            Image previousColor,
            TMP_InputField redInput,
            TMP_InputField greenInput,
            TMP_InputField blueInput,
            TMP_InputField hueInput,
            TMP_InputField saturationInput,
            TMP_InputField valueInput,
            TMP_InputField alphaInput,
            TMP_InputField hexInput,
            Button closeButton)
        {
            var serialized = new SerializedObject(editor);
            serialized.FindProperty("saturationValueField").objectReferenceValue = saturationValueField;
            serialized.FindProperty("saturationValueHandle").objectReferenceValue = saturationValueHandle;
            serialized.FindProperty("hueField").objectReferenceValue = hueField;
            serialized.FindProperty("hueHandle").objectReferenceValue = hueHandle;
            serialized.FindProperty("alphaField").objectReferenceValue = alphaField;
            serialized.FindProperty("alphaHandle").objectReferenceValue = alphaHandle;
            serialized.FindProperty("previewColor").objectReferenceValue = previewColor;
            serialized.FindProperty("previousColor").objectReferenceValue = previousColor;
            serialized.FindProperty("redInput").objectReferenceValue = redInput;
            serialized.FindProperty("greenInput").objectReferenceValue = greenInput;
            serialized.FindProperty("blueInput").objectReferenceValue = blueInput;
            serialized.FindProperty("hueInput").objectReferenceValue = hueInput;
            serialized.FindProperty("saturationInput").objectReferenceValue = saturationInput;
            serialized.FindProperty("valueInput").objectReferenceValue = valueInput;
            serialized.FindProperty("alphaInput").objectReferenceValue = alphaInput;
            serialized.FindProperty("hexInput").objectReferenceValue = hexInput;
            serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
            serialized.FindProperty("contentRoot").objectReferenceValue = editor.transform.Find("Content") as RectTransform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_InputField CreateLabeledInput(Transform parent, string label, string inputName, Vector2 anchoredPosition, float width = 108f)
        {
            RectTransform group = CreateRect("Field_" + label, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            group.anchoredPosition = anchoredPosition;
            group.sizeDelta = new Vector2(width, 40f);

            TMP_Text labelText = CreateLabel("Text (TMP)_Label_" + label, group, label, 12f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(labelText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -18f), new Vector2(34f, 0f));

            TMP_InputField input = CreateInput(inputName, group);
            SetRect(input.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(36f, -34f), new Vector2(0f, -4f));
            return input;
        }

        private static TMP_InputField CreateInput(string name, Transform parent)
        {
            GameObject inputObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            inputObject.transform.SetParent(parent, false);

            Image image = inputObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.17f, 0.18f, 1f);

            RectTransform textArea = CreateRect("Text Area", inputObject.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            textArea.offsetMin = new Vector2(6f, 2f);
            textArea.offsetMax = new Vector2(-6f, -2f);

            TMP_Text text = CreateLabel("Text", textArea, string.Empty, 12f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            text.color = Color.white;
            text.raycastTarget = false;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            TMP_InputField input = inputObject.GetComponent<TMP_InputField>();
            input.textViewport = textArea;
            input.textComponent = text;
            input.pointSize = 12f;
            input.text = "0";
            input.contentType = TMP_InputField.ContentType.Standard;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        private static TMP_Text CreateLabel(string name, Transform parent, string text, float fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static RawImage CreateRawImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            RawImage image = imageObject.GetComponent<RawImage>();
            image.color = color;
            return image;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.19f, 0.29f, 0.88f, 1f);

            TMP_Text text = CreateLabel("Text (TMP)", buttonObject.transform, label, 12f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            return buttonObject.GetComponent<Button>();
        }

        private static RectTransform CreateHandle(string name, Transform parent, float size, Color color)
        {
            Image handle = CreateImage(name, parent, color);
            handle.raycastTarget = false;
            RectTransform rect = handle.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            return rect;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            RectTransform rect = rectObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            return rect;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
