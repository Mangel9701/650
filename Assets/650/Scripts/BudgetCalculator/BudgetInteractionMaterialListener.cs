using InteractionSystem;
using UnityEngine;

namespace Studio650.Budget
{
    public class BudgetInteractionMaterialListener : MonoBehaviour
    {
        [SerializeField] private BudgetCalculatorManager manager;

        private void Awake()
        {
            if (manager == null)
                manager = BudgetCalculatorManager.GetOrCreate();
        }

        private void OnEnable()
        {
            InteractObject.OnAnyInteract.AddListener(HandleInteraction);
        }

        private void OnDisable()
        {
            InteractObject.OnAnyInteract.RemoveListener(HandleInteraction);
        }

        private void HandleInteraction(InteractObject interactObject)
        {
            if (interactObject == null || manager == null || manager.Catalog == null)
                return;

            var explicitOption = interactObject.GetComponent<MaterialCostOption>();
            if (explicitOption != null && explicitOption.Option != null)
                return;

            var transferHandler = interactObject.GetComponent<MaterialTransferHandler>();
            if (transferHandler == null)
                transferHandler = interactObject.GetComponentInChildren<MaterialTransferHandler>();

            if (transferHandler == null)
                return;

            Material selectedMaterial = transferHandler.CurrentSourceMaterial;
            BudgetMaterialOptionSO option = manager.Catalog.FindByMaterialName(transferHandler.CurrentSourceMaterialName);
            if (option != null)
                manager.SetSelection(option, selectedMaterial);
        }
    }
}
