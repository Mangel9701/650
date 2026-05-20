using InteractionSystem;
using UnityEngine;
using UnityEngine.Scripting;

namespace Studio650.Budget
{
    [Preserve]
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
            MaterialTransferHandler.AnyTransferComplete += HandleMaterialTransfer;
        }

        private void OnDisable()
        {
            InteractObject.OnAnyInteract.RemoveListener(HandleInteraction);
            MaterialTransferHandler.AnyTransferComplete -= HandleMaterialTransfer;
        }

        private void HandleInteraction(InteractObject interactObject)
        {
            if (interactObject == null || manager == null || manager.Catalog == null)
                return;

            var explicitOption = interactObject.GetComponentInChildren<MaterialCostOption>();
            if (explicitOption != null && explicitOption.Option != null)
            {
                explicitOption.NotifySelection();
                return;
            }

            var transferHandler = interactObject.GetComponent<MaterialTransferHandler>();
            if (transferHandler == null)
                transferHandler = interactObject.GetComponentInChildren<MaterialTransferHandler>();

            if (transferHandler == null)
                return;

            HandleMaterialTransfer(transferHandler);
        }

        private void HandleMaterialTransfer(MaterialTransferHandler transferHandler)
        {
            if (transferHandler == null || manager == null || manager.Catalog == null)
                return;

            var explicitOption = transferHandler.GetComponent<MaterialCostOption>();
            if (explicitOption == null)
                explicitOption = transferHandler.GetComponentInParent<MaterialCostOption>();

            if (explicitOption != null && explicitOption.Option != null)
            {
                explicitOption.NotifySelection();
                return;
            }

            Material selectedMaterial = transferHandler.CurrentSourceMaterial;
            BudgetMaterialOptionSO option = manager.Catalog.FindByMaterialName(transferHandler.CurrentSourceMaterialName);
            if (option != null)
                manager.SetSelection(option, selectedMaterial);
        }
    }
}
