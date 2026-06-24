using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InteractionSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Studio650.Budget
{
    [Preserve]
    public class BudgetCalculatorManager : MonoBehaviour
    {
        private const string DefaultCatalogResourcePath = "Budget/BudgetCalculatorCatalog";

        [SerializeField] private BudgetCalculatorCatalogSO catalog;

        private readonly Dictionary<string, BudgetMaterialSelection> selectionsByCategory = new Dictionary<string, BudgetMaterialSelection>();
        private Coroutine applySelectionsRoutine;
        private bool sceneEventsSubscribed;
        private bool singletonInitialized;

        public static BudgetCalculatorManager Instance { get; private set; }
        public event Action<IReadOnlyList<BudgetMaterialSelection>, int> SelectionChanged;

        public BudgetCalculatorCatalogSO Catalog => catalog;

        public static BudgetCalculatorManager GetOrCreate()
        {
            if (Instance != null)
                return Instance;

            var existing = FindAnyObjectByType<BudgetCalculatorManager>();
            if (existing != null)
            {
                existing.InitializeSingleton();
                return existing;
            }

            var managerObject = new GameObject("BudgetCalculatorManager");
            return managerObject.AddComponent<BudgetCalculatorManager>();
        }

        public void SetSelection(BudgetMaterialOptionSO option)
        {
            SetSelection(option, option != null ? option.Material : null);
        }

        public void SetSelection(BudgetMaterialOptionSO option, Material runtimeMaterial)
        {
            if (option == null)
                return;

            EnsureInitialized();
            selectionsByCategory[option.CategoryId] = new BudgetMaterialSelection(option, runtimeMaterial);
            NotifyChanged();
        }

        public IReadOnlyList<BudgetMaterialSelection> GetCurrentSelections()
        {
            EnsureInitialized();
            return selectionsByCategory.Values
                .Where(selection => selection.Option != null)
                .OrderBy(selection => selection.Option.CategoryOrder)
                .ThenBy(selection => selection.Option.CategoryDisplayName)
                .ToList();
        }

        public int GetTotalEuros()
        {
            EnsureInitialized();
            return selectionsByCategory.Values.Sum(selection => selection.PriceEuros);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitializeSingleton();
        }

        private void OnEnable()
        {
            if (Instance == this)
                SubscribeSceneEvents();
        }

        private void OnDisable()
        {
            UnsubscribeSceneEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                UnsubscribeSceneEvents();
                Instance = null;
            }
        }

        private void Start()
        {
            NotifyChanged();
            ScheduleApplySelectionsToScene();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Instance = null;
        }

        public void ApplySelectionsToScene()
        {
            EnsureInitialized();

            if (catalog == null || selectionsByCategory.Count == 0)
                return;

            MaterialTransferHandler[] transferHandlers = FindObjectsByType<MaterialTransferHandler>(FindObjectsInactive.Include);

            foreach (MaterialTransferHandler transferHandler in transferHandlers)
            {
                if (transferHandler == null || !transferHandler.HasValidSource)
                    continue;

                if (!BudgetMaterialOptionResolver.TryResolve(
                        transferHandler,
                        catalog,
                        out BudgetMaterialOptionSO handlerOption,
                        out Material runtimeMaterial))
                {
                    continue;
                }

                if (!selectionsByCategory.TryGetValue(handlerOption.CategoryId, out BudgetMaterialSelection selection))
                    continue;

                if (!IsSameOption(selection.Option, handlerOption))
                    continue;

                Material materialToApply = runtimeMaterial != null ? runtimeMaterial : transferHandler.CurrentSourceMaterial;
                ApplySelectionTint(selection, materialToApply);

                if (transferHandler.HasValidTargets)
                    transferHandler.TransferMaterial();
            }

            NotifyChanged();
        }

        private void InitializeSingleton()
        {
            if (singletonInitialized)
                return;

            Instance = this;
            singletonInitialized = true;

            if (transform.parent != null)
                transform.SetParent(null);

            DontDestroyOnLoad(gameObject);
            EnsureInitialized();
            SubscribeSceneEvents();
        }

        private void SubscribeSceneEvents()
        {
            if (sceneEventsSubscribed)
                return;

            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneEventsSubscribed = true;
        }

        private void UnsubscribeSceneEvents()
        {
            if (!sceneEventsSubscribed)
                return;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            sceneEventsSubscribed = false;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ScheduleApplySelectionsToScene();
        }

        private void ScheduleApplySelectionsToScene()
        {
            if (!isActiveAndEnabled)
                return;

            if (applySelectionsRoutine != null)
                StopCoroutine(applySelectionsRoutine);

            applySelectionsRoutine = StartCoroutine(ApplySelectionsToSceneNextFrame());
        }

        private IEnumerator ApplySelectionsToSceneNextFrame()
        {
            yield return null;
            yield return null;

            applySelectionsRoutine = null;
            ApplySelectionsToScene();
        }

        private void EnsureInitialized()
        {
            if (catalog == null)
                catalog = Resources.Load<BudgetCalculatorCatalogSO>(DefaultCatalogResourcePath);

            if (catalog == null || selectionsByCategory.Count > 0)
                return;

            foreach (var option in catalog.GetDefaultOptions())
                selectionsByCategory[option.CategoryId] = new BudgetMaterialSelection(option, option.Material);
        }

        private void NotifyChanged()
        {
            SelectionChanged?.Invoke(GetCurrentSelections(), GetTotalEuros());
        }

        private static void ApplySelectionTint(BudgetMaterialSelection selection, Material material)
        {
            if (!selection.HasPreviewTint || material == null)
                return;

            if (material.HasColor("_BaseColor"))
            {
                material.SetColor("_BaseColor", selection.PreviewTint);
                return;
            }

            if (material.HasColor("_Color"))
                material.SetColor("_Color", selection.PreviewTint);
        }

        private static bool IsSameOption(BudgetMaterialOptionSO left, BudgetMaterialOptionSO right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left == null || right == null)
                return false;

            return string.Equals(left.CategoryId, right.CategoryId, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(left.TechnicalMaterialName, right.TechnicalMaterialName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
