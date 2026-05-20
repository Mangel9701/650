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
            if (manager == null || manager.Catalog == null)
                return;

            Material selectedMaterial = transferHandler.CurrentSourceMaterial;
            BudgetMaterialOptionSO option = manager.Catalog.FindByMaterialName(transferHandler.CurrentSourceMaterialName);
            if (option != null)
                manager.SetSelection(option, selectedMaterial);
        }
    }
}
