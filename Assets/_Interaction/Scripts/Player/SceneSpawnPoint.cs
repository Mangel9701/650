using UnityEngine;

[DisallowMultipleComponent]
public class SceneSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnID;
    [SerializeField] private bool defaultSpawn;

    public string SpawnID => string.IsNullOrWhiteSpace(spawnID) ? gameObject.name : spawnID;
    public bool IsDefaultSpawn => defaultSpawn;
}
