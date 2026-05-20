using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Studio650.Budget
{
    public class BudgetCalculatorManager : MonoBehaviour
    {
        private const string DefaultCatalogResourcePath = "Budget/BudgetCalculatorCatalog";

        [SerializeField] private BudgetCalculatorCatalogSO catalog;

        private readonly Dictionary<string, BudgetMaterialSelection> selectionsByCategory = new Dictionary<string, BudgetMaterialSelection>();

        public static BudgetCalculatorManager Instance { get; private set; }
        public event Action<IReadOnlyList<BudgetMaterialSelection>, int> SelectionChanged;

        public BudgetCalculatorCatalogSO Catalog => catalog;

        public static BudgetCalculatorManager GetOrCreate()
        {
            if (Instance != null)
                return Instance;

            var existing = FindFirstObjectByType<BudgetCalculatorManager>();
            if (existing != null)
                return existing;

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

            Instance = this;
            EnsureInitialized();
        }

        private void Start()
        {
            NotifyChanged();
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
    }
}
