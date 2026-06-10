using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Studio650.ColorField
{
    [Preserve]
    [DisallowMultipleComponent]
    public class BaseColorFieldPanelOpener : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelPrefab;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Transform panelParent;
        [SerializeField] private bool reuseOpenPanel = true;

        [Header("Objetivo de prueba")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private int materialIndex;
        [SerializeField] private string colorPropertyName = "_BaseColor";

        private GameObject panelInstance;

        public void OpenPanel()
        {
            if (panelPrefab == null)
            {
                Debug.LogWarning($"[BaseColorFieldPanelOpener] No hay prefab de panel asignado en '{name}'.", this);
                return;
            }

            if (reuseOpenPanel && panelInstance != null)
            {
                panelInstance.SetActive(true);
                return;
            }

            Transform parent = ResolvePanelParent();
            panelInstance = Instantiate(panelPrefab, parent, false);
            panelInstance.name = panelPrefab.name;

            CenterPanel(panelInstance);
            ConfigureMaterialApplier(panelInstance);
            panelInstance.SetActive(true);
        }

        public void ClosePanel()
        {
            if (panelInstance == null)
                return;

            panelInstance.SetActive(false);
        }

        public void TogglePanel()
        {
            if (panelInstance == null || !panelInstance.activeSelf)
                OpenPanel();
            else
                ClosePanel();
        }

        public void SetTargetRenderer(Renderer renderer)
        {
            targetRenderer = renderer;
            ConfigureMaterialApplier(panelInstance);
        }

        public void SetColorPropertyName(string propertyName)
        {
            colorPropertyName = string.IsNullOrWhiteSpace(propertyName) ? "_BaseColor" : propertyName;
            ConfigureMaterialApplier(panelInstance);
        }

        private Transform ResolvePanelParent()
        {
            if (panelParent != null)
                return panelParent;

            if (targetCanvas == null)
                targetCanvas = FindAnyObjectByType<Canvas>();

            if (targetCanvas == null)
                targetCanvas = CreateRuntimeCanvas();

            return targetCanvas.transform;
        }

        private Canvas CreateRuntimeCanvas()
        {
            var canvasObject = new GameObject("Runtime_ColorFieldCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private void ConfigureMaterialApplier(GameObject panel)
        {
            if (panel == null)
                return;

            BaseColorMaterialApplier applier = panel.GetComponent<BaseColorMaterialApplier>();
            if (applier == null)
                return;

            applier.SetTarget(targetRenderer, materialIndex);
            applier.ColorPropertyName = colorPropertyName;
        }

        private static void CenterPanel(GameObject panel)
        {
            if (panel == null || panel.transform is not RectTransform rectTransform)
                return;

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }
    }
}
