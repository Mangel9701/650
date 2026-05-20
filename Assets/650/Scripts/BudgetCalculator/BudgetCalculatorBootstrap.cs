using UnityEngine;

namespace Studio650.Budget
{
    public static class BudgetCalculatorBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            BudgetCalculatorManager manager = BudgetCalculatorManager.GetOrCreate();
            if (manager.GetComponent<BudgetInteractionMaterialListener>() == null)
                manager.gameObject.AddComponent<BudgetInteractionMaterialListener>();

            var panel = GameObject.Find("Panel_Calculadora") ?? GameObject.Find("Panel Calculadora");
            if (panel != null && panel.GetComponent<BudgetCalculatorUI>() == null)
                panel.AddComponent<BudgetCalculatorUI>();
        }
    }
}
