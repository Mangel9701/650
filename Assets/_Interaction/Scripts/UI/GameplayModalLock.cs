using UnityEngine;

public static class GameplayModalLock
{
    private static Object owner;

    public static bool IsLocked => owner != null;

    public static bool CanInteract(Object requester = null)
    {
        return owner == null || owner == requester;
    }

    public static bool TryAcquire(Object requester)
    {
        if (requester == null)
            return false;

        if (owner != null && owner != requester)
            return false;

        owner = requester;
        HideInteractionPrompts();
        return true;
    }

    public static void Release(Object requester)
    {
        if (owner == null || owner != requester)
            return;

        owner = null;
    }

    public static void HideInteractionPrompts()
    {
        if (UIIngameManager.Instance == null)
            return;

        UIIngameManager.Instance.HideInteractPrompt(true);
        UIIngameManager.Instance.HideInteractPrompt(false);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        owner = null;
    }
}
