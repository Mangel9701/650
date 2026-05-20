using UnityEngine;

namespace Studio650.Budget
{
    [CreateAssetMenu(fileName = "BudgetMaterialOption", menuName = "650/Budget/Material Option")]
    public class BudgetMaterialOptionSO : ScriptableObject
    {
        [Header("Categoria")]
        [SerializeField] private string categoryId = "piso";
        [SerializeField] private string categoryDisplayName = "Piso";
        [SerializeField] private int categoryOrder;

        [Header("Material")]
        [SerializeField] private Material material;
        [SerializeField] private string technicalMaterialName;
        [SerializeField] private string displayName;

        [TextArea(2, 4)]
        [SerializeField] private string description = "Material base incluido";

        [Header("Precio")]
        [Min(0)]
        [SerializeField] private int priceEuros;
        [SerializeField] private bool defaultOption;

        public string CategoryId => string.IsNullOrWhiteSpace(categoryId) ? categoryDisplayName : categoryId;
        public string CategoryDisplayName => string.IsNullOrWhiteSpace(categoryDisplayName) ? CategoryId : categoryDisplayName;
        public int CategoryOrder => categoryOrder;
        public Material Material => material;
        public string TechnicalMaterialName => ResolveTechnicalName();
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? TechnicalMaterialName : displayName;
        public string Description => description;
        public int PriceEuros => priceEuros;
        public bool DefaultOption => defaultOption;

        public bool MatchesMaterialName(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName))
                return false;

            string normalized = NormalizeMaterialName(materialName);
            return string.Equals(normalized, NormalizeMaterialName(TechnicalMaterialName), System.StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeMaterialName(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName))
                return string.Empty;

            const string instanceSuffix = " (Instance)";
            string value = materialName.Trim();
            if (value.EndsWith(instanceSuffix, System.StringComparison.OrdinalIgnoreCase))
                value = value.Substring(0, value.Length - instanceSuffix.Length);

            return value.Trim();
        }

        private string ResolveTechnicalName()
        {
            if (!string.IsNullOrWhiteSpace(technicalMaterialName))
                return NormalizeMaterialName(technicalMaterialName);

            return material != null ? NormalizeMaterialName(material.name) : name;
        }

        private void OnValidate()
        {
            categoryId = string.IsNullOrWhiteSpace(categoryId) ? string.Empty : categoryId.Trim().ToLowerInvariant();
            categoryDisplayName = string.IsNullOrWhiteSpace(categoryDisplayName) ? string.Empty : categoryDisplayName.Trim();
            technicalMaterialName = NormalizeMaterialName(technicalMaterialName);
            displayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim();
        }
    }
}
