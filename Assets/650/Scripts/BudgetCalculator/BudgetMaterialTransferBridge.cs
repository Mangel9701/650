using InteractionSystem;
using UnityEngine;
using UnityEngine.Scripting;

namespace Studio650.Budget
{
    [Preserve]
    public static class BudgetMaterialTransferBridge
    {
        public static void NotifyTransfer(MaterialTransferHandler transferHandler)
        {
            if (transferHandler == null)
                return;

            BudgetCalculatorManager manager = BudgetCalculatorManager.GetOrCreate();
            if (manager == null)
                return;

            if (BudgetMaterialOptionResolver.TryResolve(transferHandler, manager.Catalog, out BudgetMaterialOptionSO option, out Material selectedMaterial))
                manager.SetSelection(option, selectedMaterial);
        }
    }
}
