using InteractionSystem;
using UnityEngine;
using UnityEngine.Scripting;

namespace Studio650.Budget
{
    [Preserve]
    [DisallowMultipleComponent]
    public class MaterialCostOption : MonoBehaviour
    {
        [SerializeField] private BudgetMaterialOptionSO option;
        [SerializeField] private MaterialTransferHandler materialTransferHandler;
        [SerializeField] private bool notifyOnTransferComplete = true;

        public BudgetMaterialOptionSO Option => option;

        public void NotifySelection()
        {
            if (option == null)
            {
                Debug.LogWarning($"[MaterialCostOption] No hay opcion de presupuesto asignada en {name}.", this);
                return;
            }

            Material runtimeMaterial = materialTransferHandler != null ? materialTransferHandler.CurrentSourceMaterial : option.Material;
            BudgetCalculatorManager.GetOrCreate().SetSelection(option, runtimeMaterial);
        }

        private void Reset()
        {
            materialTransferHandler = GetComponent<MaterialTransferHandler>();
        }

        private void Awake()
        {
            if (materialTransferHandler == null)
                materialTransferHandler = GetComponent<MaterialTransferHandler>();
        }

        private void OnEnable()
        {
            if (notifyOnTransferComplete && materialTransferHandler != null)
                materialTransferHandler.onTransferComplete.AddListener(NotifySelection);
        }

        private void OnDisable()
        {
            if (materialTransferHandler != null)
                materialTransferHandler.onTransferComplete.RemoveListener(NotifySelection);
        }
    }
}
