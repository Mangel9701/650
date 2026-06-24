using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1000)]
public class SceneSpawnManager : MonoBehaviour
{
    private const string LoadingSceneName = "LoadingScreen";

    [SerializeField] private bool projectSpawnToNavMesh = true;
    [SerializeField, Min(0.01f)] private float spawnNavMeshSampleRadius = 2f;
    [SerializeField] private int spawnNavMeshAreaMask = -1;
    [SerializeField] private bool treatSpawnPointAsGroundPosition = true;

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

        Vector3 spawnPosition = ResolvePlayerRootPosition(player, spawn);
        Quaternion bodyRotation = Quaternion.Euler(0f, spawn.eulerAngles.y, 0f);
        float cameraPitch = spawn.eulerAngles.x;

        FirstPersonMovement movement = player.GetComponent<FirstPersonMovement>();
        if (movement != null)
        {
            movement.TeleportTo(spawnPosition, bodyRotation, cameraPitch);
            return;
        }

        TeleportTransform(player, spawnPosition, bodyRotation);
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

    private Vector3 ResolvePlayerRootPosition(GameObject player, Transform spawn)
    {
        Vector3 groundPosition = spawn.position;
        if (projectSpawnToNavMesh && NavMesh.SamplePosition(spawn.position, out NavMeshHit hit, spawnNavMeshSampleRadius, spawnNavMeshAreaMask))
            groundPosition = hit.position;

        if (!treatSpawnPointAsGroundPosition)
            return groundPosition;

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
            return groundPosition;

        float scaleY = Mathf.Abs(player.transform.lossyScale.y);
        if (scaleY <= Mathf.Epsilon)
            scaleY = 1f;

        float rootOffsetFromGround = (controller.height * 0.5f - controller.center.y) * scaleY;
        return groundPosition + Vector3.up * rootOffsetFromGround;
    }

    private void TeleportTransform(GameObject player, Vector3 spawnPosition, Quaternion bodyRotation)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;

        if (controller != null)
            controller.enabled = false;

        player.transform.SetPositionAndRotation(spawnPosition, bodyRotation);

        if (controller != null)
            controller.enabled = controllerWasEnabled;
    }
}
