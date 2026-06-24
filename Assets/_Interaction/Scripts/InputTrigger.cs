using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputTrigger : MonoBehaviour
{
    [Header("Configuraci\u00f3n")]
    [SerializeField] private InputActionReference actionReference;
    [SerializeField] private bool debugTrigger = false;
    [SerializeField] private bool ignoreGameplayModalLock = false;

    [Header("Eventos")]
    public UnityEvent onTrigger;

    private void OnEnable()
    {
        if (actionReference != null && actionReference.action != null)
        {
            actionReference.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (actionReference != null && actionReference.action != null)
        {
            actionReference.action.Disable();
        }
    }

    private void Update()
    {
        if (actionReference != null && actionReference.action != null)
        {
            if (actionReference.action.WasPerformedThisFrame())
            {
                if (GameplayModalLock.IsLocked && !ignoreGameplayModalLock)
                {
                    if (debugTrigger)
                    {
                        Debug.Log($"[InputTrigger] Acci\u00f3n '{actionReference.action.name}' bloqueada por modal abierto.", this);
                    }

                    return;
                }

                if (debugTrigger)
                {
                    Debug.Log($"[InputTrigger] Acci\u00f3n '{actionReference.action.name}' detectada.", this);
                }
                onTrigger?.Invoke();
            }
        }
    }
}
