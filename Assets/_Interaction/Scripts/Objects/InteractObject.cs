using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class InteractObjectUnityEvent : UnityEvent<InteractObject> { }

public class InteractObject : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private bool debugInteraction = false;
    public bool stopPlayerMovementOnInteract = false;

    [Header("Punto de interacción / mirada")]
    [SerializeField] private Vector3 eyeOffset = Vector3.zero;

    [Header("Eventos locales")]
    public UnityEvent onInteract;

    [Header("Evento local con referencia")]
    public InteractObjectUnityEvent onInteractWithReference;

    public static InteractObjectUnityEvent OnAnyInteract = new InteractObjectUnityEvent();

    public bool CanInteract => canInteract;
    public Vector3 EyeOffset => eyeOffset;

    public void OnInteract()
    {
        if (!canInteract)
            return;

        if (debugInteraction)
        {
            Debug.Log($"[InteractObject] Interacción con: {name}", this);
        }

        onInteract?.Invoke();
        onInteractWithReference?.Invoke(this);
        OnAnyInteract?.Invoke(this);
    }

    public void SetEyeOffset(Vector3 value)
    {
        eyeOffset = value;
    }

    public void SetCanInteract(bool value)
    {
        canInteract = value;
    }

    public void EnableInteraction()
    {
        canInteract = true;
    }

    public void DisableInteraction()
    {
        canInteract = false;
    }
}