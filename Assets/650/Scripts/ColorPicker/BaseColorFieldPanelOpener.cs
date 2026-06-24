using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;
using UnityEngine.UI;
using InteractionSystem;
using System;

namespace Studio650.ColorField
{
    [Preserve]
    [DisallowMultipleComponent]
    public class BaseColorFieldPanelOpener : MonoBehaviour
    {
        private const string RuntimeCanvasName = "Runtime_ColorFieldCanvas";

        [Header("Panel")]
        [SerializeField] private GameObject panelPrefab;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private Transform panelParent;
        [SerializeField] private bool reuseOpenPanel = true;

        [Header("Objetivo de color")]
        [SerializeField] private MaterialTransferHandler targetTransferHandler;
        [SerializeField] private string colorPropertyName = "_BaseColor";

        private GameObject panelInstance;

        public void OpenPanel()
        {
            if (panelPrefab == null)
            {
                Debug.LogWarning($"[BaseColorFieldPanelOpener] No hay prefab de panel asignado en '{name}'.", this);
                return;
            }

            if (reuseOpenPanel && panelInstance == null)
                panelInstance = FindExistingScenePanel();

            if (reuseOpenPanel && panelInstance != null)
            {
                ShowPanel(panelInstance, false);
                return;
            }

            Transform parent = ResolvePanelParent();
            panelInstance = Instantiate(panelPrefab, parent, false);
            panelInstance.name = panelPrefab.name;

            ShowPanel(panelInstance, true);
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

        public void SetTargetTransferHandler(MaterialTransferHandler transferHandler)
        {
            targetTransferHandler = transferHandler;
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

            if (targetCanvas != null && IsScreenSpaceCanvas(targetCanvas))
                return targetCanvas.transform;

            if (targetCanvas != null)
                Debug.LogWarning($"[BaseColorFieldPanelOpener] El canvas asignado en '{name}' es World Space. Se buscara un canvas de HUD.", this);

            targetCanvas = FindPreferredScreenSpaceCanvas();
            if (targetCanvas == null)
                targetCanvas = CreateRuntimeCanvas();

            return targetCanvas.transform;
        }

        private GameObject FindExistingScenePanel()
        {
            BaseColorFieldEditorUI[] panels = FindObjectsByType<BaseColorFieldEditorUI>(FindObjectsInactive.Include);
            for (int i = 0; i < panels.Length; i++)
            {
                BaseColorFieldEditorUI panel = panels[i];
                if (panel == null || panel.gameObject == null)
                    continue;

                GameObject panelObject = panel.gameObject;
                if (!panelObject.scene.IsValid() || !panelObject.scene.isLoaded)
                    continue;

                if (!MatchesPanelPrefabName(panelObject))
                    continue;

                if (!IsUnderScreenSpaceCanvas(panelObject.transform))
                    continue;

                return panelObject;
            }

            return null;
        }

        private bool MatchesPanelPrefabName(GameObject panelObject)
        {
            if (panelPrefab == null)
                return true;

            return panelObject.name.StartsWith(panelPrefab.name, StringComparison.Ordinal);
        }

        private static Canvas FindPreferredScreenSpaceCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Canvas bestCanvas = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || !canvas.gameObject.scene.IsValid() || !canvas.gameObject.scene.isLoaded)
                    continue;

                Canvas rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
                if (!IsScreenSpaceCanvas(rootCanvas))
                    continue;

                int score = GetCanvasScore(rootCanvas);
                if (score <= bestScore)
                    continue;

                bestCanvas = rootCanvas;
                bestScore = score;
            }

            return bestCanvas;
        }

        private static int GetCanvasScore(Canvas canvas)
        {
            int score = canvas.gameObject.activeInHierarchy ? 10000 : 0;
            string lowerName = canvas.name.ToLowerInvariant();

            if (lowerName.Contains("hud"))
                score += 1000;

            if (lowerName.Contains("ui"))
                score += 500;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                score += 100;

            score += canvas.sortingOrder;
            return score;
        }

        private static bool IsUnderScreenSpaceCanvas(Transform panelTransform)
        {
            Canvas canvas = panelTransform.GetComponentInParent<Canvas>(true);
            if (canvas == null)
                return false;

            Canvas rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            return IsScreenSpaceCanvas(rootCanvas);
        }

        private static bool IsScreenSpaceCanvas(Canvas canvas)
        {
            return canvas != null && canvas.renderMode != RenderMode.WorldSpace;
        }

        private void ShowPanel(GameObject panel, bool center)
        {
            if (panel == null)
                return;

            if (center)
                CenterPanel(panel);

            panel.transform.SetAsLastSibling();
            ConfigureMaterialApplier(panel);
            panel.SetActive(true);
        }

        private Canvas CreateRuntimeCanvas()
        {
            var canvasObject = new GameObject(RuntimeCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

            if (targetTransferHandler == null)
            {
                Debug.LogWarning($"[BaseColorFieldPanelOpener] No hay MaterialTransferHandler asignado en '{name}'.", this);
                return;
            }

            applier.SetTarget(targetTransferHandler);
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
