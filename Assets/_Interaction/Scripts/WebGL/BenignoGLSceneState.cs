using UnityEngine;
using UnityEngine.SceneManagement;

public static class BenignoGLSceneState
{
    private const string LoadingScene = "LoadingScreen";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneChanged;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;

        SceneManager.sceneLoaded += OnSceneChanged;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        UpdateSceneReadyState();
    }

    private static void OnSceneChanged(Scene scene, LoadSceneMode mode)
    {
        UpdateSceneReadyState();
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        UpdateSceneReadyState();
    }

    private static void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        UpdateSceneReadyState();
    }

    public static void UpdateSceneReadyState()
    {
        BenignoGLWebBridge.SetGameplaySceneReady(IsGameplaySceneReady());
    }

    private static bool IsGameplaySceneReady()
    {
        if (!IsLoadingScreenReady())
            return false;

        AdditiveSceneLoader additiveLoader = Object.FindFirstObjectByType<AdditiveSceneLoader>();
        if (additiveLoader == null)
            return IsAnyNonLoadingSceneLoaded();

        if (!additiveLoader.IsDone)
            return false;

        foreach (string subSceneKey in additiveLoader.subSceneKeys)
        {
            if (!IsSceneLoaded(subSceneKey))
                return false;
        }

        return true;
    }

    private static bool IsLoadingScreenReady()
    {
        LoadingScreen loadingScreen = Object.FindFirstObjectByType<LoadingScreen>();
        return loadingScreen == null || LoadingScreen.IsSceneReady;
    }

    private static bool IsAnyNonLoadingSceneLoaded()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.name != LoadingScene)
                return true;
        }

        return false;
    }

    private static bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.name == sceneName)
            {
                return true;
            }
        }

        return false;
    }
}
