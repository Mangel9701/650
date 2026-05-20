using System.Collections.Generic;
using UnityEngine;

namespace Studio650.Budget
{
    [CreateAssetMenu(fileName = "BudgetCalculatorCatalog", menuName = "650/Budget/Calculator Catalog")]
    public class BudgetCalculatorCatalogSO : ScriptableObject
    {
        [SerializeField] private List<BudgetMaterialOptionSO> options = new List<BudgetMaterialOptionSO>();

        public IReadOnlyList<BudgetMaterialOptionSO> Options => options;

        public IEnumerable<BudgetMaterialOptionSO> GetDefaultOptions()
        {
            var seenCategories = new HashSet<string>();

            foreach (var option in options)
            {
                if (option == null || !option.DefaultOption)
                    continue;

                seenCategories.Add(option.CategoryId);
                yield return option;
            }

            foreach (var option in options)
            {
                if (option == null || seenCategories.Contains(option.CategoryId))
                    continue;

                seenCategories.Add(option.CategoryId);
                yield return option;
            }
        }

        public BudgetMaterialOptionSO FindByMaterialName(string materialName)
        {
            foreach (var option in options)
            {
                if (option != null && option.MatchesMaterialName(materialName))
                    return option;
            }

            return null;
        }
    }
}
