using InteractionSystem;
using UnityEngine;
using UnityEngine.Scripting;

namespace Studio650.Budget
{
    [Preserve]
    internal static class BudgetMaterialOptionResolver
    {
        public static bool TryResolve(
            MaterialTransferHandler transferHandler,
            BudgetCalculatorCatalogSO catalog,
            out BudgetMaterialOptionSO option,
            out Material runtimeMaterial)
        {
            option = null;
            runtimeMaterial = null;

            if (transferHandler == null)
                return false;

            runtimeMaterial = transferHandler.CurrentSourceMaterial;

            MaterialCostOption explicitOption = FindExplicitOption(transferHandler);
            if (explicitOption != null && explicitOption.Option != null)
            {
                option = explicitOption.Option;
                return true;
            }

            if (catalog == null)
                return false;

            option = catalog.FindByMaterialName(transferHandler.CurrentSourceMaterialName);
            return option != null;
        }

        private static MaterialCostOption FindExplicitOption(MaterialTransferHandler transferHandler)
        {
            MaterialCostOption explicitOption = transferHandler.GetComponent<MaterialCostOption>();
            if (explicitOption != null)
                return explicitOption;

            explicitOption = transferHandler.GetComponentInParent<MaterialCostOption>();
            if (explicitOption != null)
                return explicitOption;

            return transferHandler.GetComponentInChildren<MaterialCostOption>();
        }
    }
}
