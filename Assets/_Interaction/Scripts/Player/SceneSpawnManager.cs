using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1000)]
public class SceneSpawnManager : MonoBehaviour
{
    private const string LoadingSceneName = "LoadingScreen";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        TryCreateForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
            TryCreateForScene(scene);
    }

    private static void TryCreateForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name == LoadingSceneName)
            return;

        if (FindFirstObjectByType<SceneSpawnManager>() != null)
            return;

        GameObject managerObject = new GameObject(nameof(SceneSpawnManager));
        managerObject.AddComponent<SceneSpawnManager>();
    }

    private IEnumerator Start()
    {
        yield return null;

        LoadingScreen loadingScreen = FindFirstObjectByType<LoadingScreen>();
        if (loadingScreen != null)
            yield return new WaitUntil(() => loadingScreen == null || LoadingScreen.IsSceneReady);

        yield return null;
        ApplyPendingSpawn();
        Destroy(gameObject);
    }

    private void ApplyPendingSpawn()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        DoorManager manager = DoorManager.EnsureInstance();
        string pendingSpawnID = manager.ConsumePendingSpawn();
        Transform spawn = ResolveSpawnPoint(pendingSpawnID);

        if (spawn == null)
        {
            if (!string.IsNullOrWhiteSpace(pendingSpawnID))
                Debug.LogWarning($"[SceneSpawnManager] No se encontro el spawn '{pendingSpawnID}'.");

            return;
        }

        FirstPersonMovement movement = player.GetComponent<FirstPersonMovement>();
        if (movement != null)
        {
            movement.TeleportTo(spawn);
            return;
        }

        TeleportTransform(player, spawn);
    }

    private Transform ResolveSpawnPoint(string spawnID)
    {
        SceneSpawnPoint[] spawnPoints = FindObjectsByType<SceneSpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (!string.IsNullOrWhiteSpace(spawnID))
        {
            foreach (SceneSpawnPoint point in spawnPoints)
            {
                if (string.Equals(point.SpawnID, spawnID, StringComparison.Ordinal))
                    return point.transform;
            }
        }

        foreach (SceneSpawnPoint point in spawnPoints)
        {
            if (point.IsDefaultSpawn)
                return point.transform;
        }

        return null;
    }

    private void TeleportTransform(GameObject player, Transform spawn)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;

        if (controller != null)
            controller.enabled = false;

        Quaternion bodyRotation = Quaternion.Euler(0f, spawn.eulerAngles.y, 0f);
        player.transform.SetPositionAndRotation(spawn.position, bodyRotation);

        if (controller != null)
            controller.enabled = controllerWasEnabled;
    }
}
