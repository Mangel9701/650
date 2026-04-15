using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Interact : MonoBehaviour
{
    [SerializeField] private InputSystem_Actions inputActions;

    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 3f;

    [Header("Layers")]
    [SerializeField] private LayerMask layerItem;
    [SerializeField] private LayerMask layerDoor;
    [SerializeField] private LayerMask occlusionLayer;

    // --- DEBUGGING VISUAL ---
    private GameObject debugLineInstance;
    private LineRenderer occlusionDebugLine;
    // ------------------------

    [SerializeField] private DoorNameDisplay doorNameDisplay;
    private DoorSceneLoader lastSeenDoor;

    [Header("Prefabs & Visuals")]
    public GameObject interactPrefab;
    public GameObject doorInteractPrefab;

    private Transform currentTarget;
    private GameObject currentInstance;

    public FirstPersonMovement firstPerson;
    public bool wasLookingAtDoor = false;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Interact.started += OnInteractPerformed;
    }

    private void Start()
    {
        GameObject hud = GameObject.Find("HUD_Manager");
        if (hud != null)
            doorNameDisplay = hud.GetComponent<DoorNameDisplay>();
        else
            Debug.LogWarning("No se encontro el objeto 'HUD_manager'");
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.started -= OnInteractPerformed;
        inputActions.Player.Disable();
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Intentando interactuar...");
        TryInteract();
    }

    public void TryInteract()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        int combinedLayerMask = layerItem  | layerDoor;

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, combinedLayerMask))
        {
            Debug.Log($"Objeto detectado: {hit.collider.name} en la capa {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            Vector3 finalPosition;
            bool isDoor = (1 << hit.collider.gameObject.layer) == layerDoor.value;

            if (TryGetOffsetWorld(hit.collider, isDoor, out finalPosition))
            {
                if (IsOccluded(finalPosition, hit.collider))
                {
                    Debug.Log("Interacción bloqueada por un objeto entre el jugador y el objetivo.");
                    return;
                }
            }

            int hitLayerMask = 1 << hit.collider.gameObject.layer;

            if ((hitLayerMask & layerItem) != 0)
            {
                firstPerson.isInteracting = true;
                hit.collider.GetComponent<ItemDisplay>()?.OnInteract();
            }
            else if ((hitLayerMask & layerDoor) != 0)
            {
                hit.collider.GetComponent<DoorSceneLoader>()?.LoadNewScene();
            }
        }
    }

    private void Update()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        int combinedLayerMask = layerItem  | layerDoor ;
        bool foundVisibleTarget = false;
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, combinedLayerMask))
        {
            Vector3 finalPosition;
            bool isDoor = (1 << hit.collider.gameObject.layer) == layerDoor.value;

            if (TryGetOffsetWorld(hit.collider, isDoor, out finalPosition))
            {
                if (!IsOccluded(finalPosition, hit.collider))
                {
                    foundVisibleTarget = true;

                    wasLookingAtDoor = isDoor;

                    if (isDoor)
                    {
                        UIIngameManager.Instance.ShowInteractPrompt(true);
                        UIIngameManager.Instance.HideInteractPrompt(false);
                        DoorSceneLoader door = hit.collider.GetComponent<DoorSceneLoader>();
                        if (door != null)
                        {
                            doorNameDisplay.UpdateDoorName(door.nombreEscenario);
                            if (door != lastSeenDoor) lastSeenDoor = door;
                        }
                    }
                    else
                    {
                        UIIngameManager.Instance.ShowInteractPrompt(false);
                        UIIngameManager.Instance.HideInteractPrompt(true);
                    }

                    if (hit.transform != currentTarget)
                    {
                        DestroyCurrentInstance();
                        currentTarget = hit.transform;
                        GameObject prefabToInstantiate = isDoor ? doorInteractPrefab : interactPrefab;
                        if (prefabToInstantiate != null)
                            currentInstance = Instantiate(prefabToInstantiate);
                    }

                    if (currentInstance != null)
                    {
                        currentInstance.transform.position = finalPosition;
                    }
                }
            }
        }

        if (!foundVisibleTarget)
        {
            UIIngameManager.Instance.HideInteractPrompt(true);
            UIIngameManager.Instance.HideInteractPrompt(false);
            DestroyCurrentInstance();
            wasLookingAtDoor = false;
            if (lastSeenDoor != null)
                lastSeenDoor = null;

            //HideDebugLine(); 
        }
    }

    private LineRenderer GetDebugLine()
    {
        if (debugLineInstance == null)
        {
            debugLineInstance = new GameObject("OcclusionDebugLine");

            debugLineInstance.transform.SetParent(Camera.main.transform);

            occlusionDebugLine = debugLineInstance.AddComponent<LineRenderer>();
            occlusionDebugLine.startWidth = 0.05f; 
            occlusionDebugLine.endWidth = 0.01f; 
            occlusionDebugLine.positionCount = 2;

            occlusionDebugLine.material = new Material(Shader.Find("Sprites/Default"));

            debugLineInstance.SetActive(false);
        }

        return occlusionDebugLine;
    }

    private void HideDebugLine()
    {
        if (debugLineInstance != null)
        {
            debugLineInstance.SetActive(false);
        }
    }
    private bool IsOccluded(Vector3 targetPosition, Collider targetCollider)
    {

        Vector3 origin = Camera.main.transform.position;
        Vector3 direction = (targetPosition - origin).normalized;
        float distance = Vector3.Distance(origin, targetPosition) - 0.01f;

        Color visibleColor = Color.cyan;
        Color occludedColor = Color.magenta;

        RaycastHit hit;
        bool isBlocked = false;
        Vector3 rayEndPosition = targetPosition; 

        if (Physics.Raycast(origin, direction, out hit, distance, occlusionLayer))
        {
            if (hit.collider != targetCollider && !hit.transform.IsChildOf(targetCollider.transform))
            {
                isBlocked = true;
                rayEndPosition = hit.point; 

                Debug.Log($"OCLUSIÓN DETECTADA: El rayo fue bloqueado por: {hit.collider.name}. Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            }
        }

        return isBlocked;
    }


    private bool TryGetOffsetWorld(Collider col, bool isDoor, out Vector3 worldPos)
    {
        worldPos = col.bounds.center;

        if (!isDoor)
        {
            if (col.TryGetComponent<ItemDisplay>(out var item))
            {
                worldPos = item.transform.TransformPoint(item.eyeOffset);
                return true;
            }
        }
        else
        {
            if (col.TryGetComponent<DoorSceneLoader>(out var door))
            {
                worldPos = col.bounds.center + door.doorIconOffset;
                return true;
            }
        }

        return false;
    }

    private void DestroyCurrentInstance()
    {
        if (currentInstance != null)
        {
            Destroy(currentInstance);
            currentInstance = null;
        }
        currentTarget = null;
    }

    private Bounds GetBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);

        foreach (Renderer rend in renderers)
            bounds.Encapsulate(rend.bounds);

        return bounds;
    }
}