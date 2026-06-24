using UnityEngine;

namespace Studio650.Budget
{
    public readonly struct BudgetMaterialSelection
    {
        public BudgetMaterialSelection(BudgetMaterialOptionSO option, Material runtimeMaterial)
        {
            Option = option;
            RuntimeMaterial = runtimeMaterial;

            if (TryResolveMaterialTint(runtimeMaterial, out Color tint) ||
                TryResolveMaterialTint(option != null ? option.Material : null, out tint))
            {
                HasPreviewTint = true;
                PreviewTint = tint;
            }
            else
            {
                HasPreviewTint = false;
                PreviewTint = Color.white;
            }
        }

        public BudgetMaterialOptionSO Option { get; }
        public Material RuntimeMaterial { get; }
        public bool HasPreviewTint { get; }
        public Color PreviewTint { get; }
        public string CategoryId => Option != null ? Option.CategoryId : string.Empty;
        public int PriceEuros => Option != null ? Option.PriceEuros : 0;

        private static bool TryResolveMaterialTint(Material material, out Color tint)
        {
            tint = Color.white;

            if (material == null)
                return false;

            if (material.HasColor("_BaseColor"))
            {
                tint = material.GetColor("_BaseColor");
                return true;
            }

            if (material.HasColor("_Color"))
            {
                tint = material.GetColor("_Color");
                return true;
            }

            return false;
        }
    }
}
