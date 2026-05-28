using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class DoorSceneLoader : MonoBehaviour
{
    [SerializeField] public string nombreEscenario;
    [SerializeField] public string doorID;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] public string sceneAddress;

    [Header("Scene Navigation")]
    [SerializeField] private string targetSpawnID;
    [SerializeField] private string returnSpawnID;
    [SerializeField] private string sourceSceneAddress;
    [SerializeField] private bool returnToPreviousScene;
    [SerializeField] private bool addReturnPointToHistory = true;
    [SerializeField] private bool loadOnTriggerEnter;

    [SerializeField] public Vector3 doorIconOffset;

    private GameObject loadingScreenInstance;
    private AsyncOperationHandle<SceneInstance> _sceneLoadHandle;
    private bool navigationPrepared;


    private void OnTriggerEnter(Collider other)
    {
        if (loadOnTriggerEnter && IsPlayer(other))
            LoadScene();
    }

    public void LoadScene()
    {
        DoorManager manager = DoorManager.EnsureInstance();

        if (returnToPreviousScene)
        {
            if (!manager.PrepareReturnSceneLoad(sceneAddress, ResolveTargetSpawnID()))
            {
                Debug.LogWarning("[DoorSceneLoader] No hay historial de retorno ni escena fallback configurada.");
                return;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(sceneAddress))
            {
                Debug.LogError("[DoorSceneLoader] No se puede cargar una escena sin address.");
                return;
            }

            manager.PrepareSceneLoad(
                sceneAddress,
                ResolveTargetSpawnID(),
                ResolveSourceSceneAddress(),
                ResolveReturnSpawnID(),
                addReturnPointToHistory);
        }

        navigationPrepared = true;
        LoadLoadingScreen();
    }

    public void LoadSceneAdressable()
    {
        LoadScene();
    }

    public void LoadLoadingScreen()
    {
        DoorManager manager = DoorManager.EnsureInstance();
        if (!navigationPrepared)
        {
            manager.SaveAdressableString(sceneAddress);
            manager.SetPendingSpawn(ResolveTargetSpawnID());
        }

        navigationPrepared = false;
        SceneManager.LoadScene("LoadingScreen");
    }

    public void LoadNewScene()
    {
        LoadScene();
    }

  
    public Transform GetSpawnPoint() => spawnPoint;

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.GetComponentInParent<FirstPersonMovement>() != null;
    }

    private string ResolveTargetSpawnID()
    {
        return string.IsNullOrWhiteSpace(targetSpawnID) ? doorID : targetSpawnID;
    }

    private string ResolveReturnSpawnID()
    {
        return string.IsNullOrWhiteSpace(returnSpawnID) ? doorID : returnSpawnID;
    }

    private string ResolveSourceSceneAddress()
    {
        return string.IsNullOrWhiteSpace(sourceSceneAddress)
            ? SceneManager.GetActiveScene().name
            : sourceSceneAddress;
    }
}
