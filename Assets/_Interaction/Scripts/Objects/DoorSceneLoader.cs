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

    [SerializeField] public Vector3 doorIconOffset;

    private GameObject loadingScreenInstance;
    private AsyncOperationHandle<SceneInstance> _sceneLoadHandle;


    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Main Player" && gameObject.name == "ColliderDoorScene")
        {
            DoorManager.Instance.SaveAdressableString(sceneAddress);
            DoorManager.Instance.LastDoorUsed = doorID;
            sceneAddress = DoorManager.Instance.GetAdressableAdress();

            LoadLoadingScreen();
        }
    }

    public void LoadSceneAdressable()
    {

        LoadLoadingScreen();
    }

    public void LoadLoadingScreen()
    {
        DoorManager.Instance.SaveAdressableString(sceneAddress);
        SceneManager.LoadScene("LoadingScreen");
    }

    public void LoadNewScene()
    {
        DoorManager.Instance.LastDoorUsed = doorID;
        LoadLoadingScreen();
    }

  
    public Transform GetSpawnPoint() => spawnPoint;
}
