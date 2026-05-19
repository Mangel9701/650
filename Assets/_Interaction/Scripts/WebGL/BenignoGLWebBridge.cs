using System.Runtime.InteropServices;

public static class BenignoGLWebBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void BenignoGL_SetGameplayPointerMode(int isEnabled);

    [DllImport("__Internal")]
    private static extern void BenignoGL_SetGameplaySceneReady(int isReady);

    [DllImport("__Internal")]
    private static extern int BenignoGL_IsGameplayFocused();
#endif

    public static void SetGameplayPointerMode(bool isEnabled)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        BenignoGL_SetGameplayPointerMode(isEnabled ? 1 : 0);
#endif
    }

    public static void SetGameplaySceneReady(bool isReady)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        BenignoGL_SetGameplaySceneReady(isReady ? 1 : 0);
#endif
    }

    public static bool IsGameplayFocused()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return BenignoGL_IsGameplayFocused() == 1;
#else
        return true;
#endif
    }
}
