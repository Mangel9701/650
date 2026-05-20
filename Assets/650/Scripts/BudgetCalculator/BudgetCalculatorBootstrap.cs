using UnityEngine;
using UnityEngine.Scripting;

namespace Studio650.Budget
{
    [Preserve]
    public static class BudgetCalculatorBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            BudgetCalculatorManager manager = BudgetCalculatorManager.GetOrCreate();
            if (manager.GetComponent<BudgetInteractionMaterialListener>() == null)
                manager.gameObject.AddComponent<BudgetInteractionMaterialListener>();

            if (manager.GetComponent<BudgetCalculatorPanelBinder>() == null)
                manager.gameObject.AddComponent<BudgetCalculatorPanelBinder>();
        }
    }
}
