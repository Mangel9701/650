using UnityEngine;

namespace Studio650.Budget
{
    public readonly struct BudgetMaterialSelection
    {
        public BudgetMaterialSelection(BudgetMaterialOptionSO option, Material runtimeMaterial)
        {
            Option = option;
            RuntimeMaterial = runtimeMaterial;
        }

        public BudgetMaterialOptionSO Option { get; }
        public Material RuntimeMaterial { get; }
        public string CategoryId => Option != null ? Option.CategoryId : string.Empty;
        public int PriceEuros => Option != null ? Option.PriceEuros : 0;
    }
}
