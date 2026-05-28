using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceProviders;

public class DoorManager : MonoBehaviour
{
    public static DoorManager Instance;
    public string LastDoorUsed { get; set; }

    public string AdressableAdress { get; set; }
    public string AddressableAddress
    {
        get => AdressableAdress;
        set => AdressableAdress = value;
    }

    public string PendingSpawnID { get; private set; }
    public SceneInstance PreviousScene { get; set; }

    public bool isAccesible = false;

    [Serializable]
    private class SceneReturnPoint
    {
        public string sceneAddress;
        public string spawnID;

        public SceneReturnPoint(string sceneAddress, string spawnID)
        {
            this.sceneAddress = sceneAddress;
            this.spawnID = spawnID;
        }
    }

    [SerializeField] private List<SceneReturnPoint> sceneHistory = new List<SceneReturnPoint>();
    [SerializeField] private List<string> storedStrings = new List<string>();

    public static DoorManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        DoorManager existing = FindFirstObjectByType<DoorManager>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject managerObject = new GameObject(nameof(DoorManager));
        Instance = managerObject.AddComponent<DoorManager>();
        return Instance;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StoreString(string newString)
    {
        storedStrings.Add(newString);
    }

    public bool ContainsString(string searchString)
    {
        return storedStrings.Contains(searchString);
    }

    public void SaveAdressableString(string AdressableAdressToSave)
    {
        AdressableAdress = AdressableAdressToSave;
    }

    public string GetAdressableAdress()
    {
        return AdressableAdress;
    }

    public void PrepareSceneLoad(
        string targetSceneAddress,
        string targetSpawnID,
        string sourceSceneAddress,
        string returnSpawnID,
        bool pushReturnPoint)
    {
        if (string.IsNullOrWhiteSpace(targetSceneAddress))
        {
            Debug.LogError("[DoorManager] No se puede cargar una escena sin address.");
            return;
        }

        if (pushReturnPoint && !string.IsNullOrWhiteSpace(sourceSceneAddress))
        {
            sceneHistory.Add(new SceneReturnPoint(sourceSceneAddress, returnSpawnID));
        }

        SaveAdressableString(targetSceneAddress);
        SetPendingSpawn(targetSpawnID);
    }

    public bool PrepareReturnSceneLoad(string fallbackSceneAddress, string fallbackSpawnID)
    {
        if (sceneHistory.Count > 0)
        {
            int lastIndex = sceneHistory.Count - 1;
            SceneReturnPoint returnPoint = sceneHistory[lastIndex];
            sceneHistory.RemoveAt(lastIndex);

            SaveAdressableString(returnPoint.sceneAddress);
            SetPendingSpawn(returnPoint.spawnID);
            return true;
        }

        if (!string.IsNullOrWhiteSpace(fallbackSceneAddress))
        {
            SaveAdressableString(fallbackSceneAddress);
            SetPendingSpawn(fallbackSpawnID);
            return true;
        }

        return false;
    }

    public string ConsumePendingSpawn()
    {
        string spawnID = PendingSpawnID;
        PendingSpawnID = string.Empty;
        return spawnID;
    }

    public void SetPendingSpawn(string spawnID)
    {
        PendingSpawnID = spawnID;
        LastDoorUsed = spawnID;
    }

    public void IsAccesibleChange(bool accesibility)
    {
        isAccesible = accesibility;
    }

    public void Restart()
    {
        storedStrings.Clear();
        sceneHistory.Clear();
        PendingSpawnID = string.Empty;
        LastDoorUsed = string.Empty;
        AdressableAdress = string.Empty;
    }

}
