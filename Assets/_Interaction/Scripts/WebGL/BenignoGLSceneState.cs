using UnityEngine;
using UnityEngine.SceneManagement;

public static class BenignoGLSceneState
{
    private const string MainScene = "650-Interaccion";
    private const string PbScene = "650-PB";
    private const string GeoScene = "650-Geo";

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
        BenignoGLWebBridge.SetGameplaySceneReady(
            IsSceneLoaded(MainScene) &&
            IsSceneLoaded(PbScene) &&
            IsSceneLoaded(GeoScene) &&
            IsLoadingScreenReady());
    }

    private static bool IsLoadingScreenReady()
    {
        return Object.FindFirstObjectByType<LoadingScreen>() == null || LoadingScreen.IsSceneReady;
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
