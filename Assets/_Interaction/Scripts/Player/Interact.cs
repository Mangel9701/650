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
    [SerializeField, HideInInspector] private LayerMask layer3D;
    [SerializeField, HideInInspector] private LayerMask layerTexture;
    [SerializeField, HideInInspector] private LayerMask layerPainting;
    [SerializeField, HideInInspector] private LayerMask layerVideo;

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
    private int noPostLayerMask;
    private int namedDoorLayerMask;

    public FirstPersonMovement firstPerson;
    public bool wasLookingAtDoor = false;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        noPostLayerMask = LayerMask.GetMask("NoPost");
        namedDoorLayerMask = LayerMask.GetMask("Door");
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
        int itemLayerMask = GetItemLayerMask();
        int doorLayerMask = GetDoorLayerMask();
        int combinedLayerMask = itemLayerMask | doorLayerMask;

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, combinedLayerMask, QueryTriggerInteraction.Collide))
        {
            Debug.Log($"Objeto detectado: {hit.collider.name} en la capa {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            Vector3 finalPosition;
            DoorSceneLoader door = hit.collider.GetComponentInParent<DoorSceneLoader>();
            bool isDoor = door != null && IsInLayerMask(hit.collider.gameObject.layer, doorLayerMask);

            if (TryGetOffsetWorld(hit.collider, isDoor, out finalPosition, out _))
            {
                if (IsOccluded(finalPosition, hit.collider))
                {
                    Debug.Log("Interacción bloqueada por un objeto entre el jugador y el objetivo.");
                    return;
                }
            }

            var interactObject = hit.collider.GetComponentInParent<InteractObject>();
            if (!isDoor && interactObject != null)
            {
                if (interactObject.stopPlayerMovementOnInteract && firstPerson != null)
                {
                    firstPerson.SetInteracting(true);
                }

                interactObject.OnInteract();


            }
            else if (isDoor)
            {
                door.LoadNewScene();
            }
        }
    }

    private void Update()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        int itemLayerMask = GetItemLayerMask();
        int doorLayerMask = GetDoorLayerMask();
        int combinedLayerMask = itemLayerMask | doorLayerMask;
        bool foundVisibleTarget = false;
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, combinedLayerMask, QueryTriggerInteraction.Collide))
        {
            Vector3 finalPosition;
            DoorSceneLoader door = hit.collider.GetComponentInParent<DoorSceneLoader>();
            bool isDoor = door != null && IsInLayerMask(hit.collider.gameObject.layer, doorLayerMask);

            if (TryGetOffsetWorld(hit.collider, isDoor, out finalPosition, out Transform targetTransform))
            {
                if (!IsOccluded(finalPosition, hit.collider))
                {
                    foundVisibleTarget = true;

                    wasLookingAtDoor = isDoor;

                    if (isDoor)
                    {
                        ShowPrompt(true);
                        HidePrompt(false);
                        if (door != null)
                        {
                            if (doorNameDisplay != null)
                                doorNameDisplay.UpdateDoorName(door.nombreEscenario);

                            if (door != lastSeenDoor) lastSeenDoor = door;
                        }
                    }
                    else
                    {
                        ShowPrompt(false);
                        HidePrompt(true);
                    }

                    if (targetTransform != currentTarget)
                    {
                        DestroyCurrentInstance();
                        currentTarget = targetTransform;
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
            HidePrompt(true);
            HidePrompt(false);
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

        if (Physics.Raycast(origin, direction, out hit, distance, occlusionLayer, QueryTriggerInteraction.Ignore))
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

    private int GetItemLayerMask()
    {
        int mask = layerItem.value
            | layer3D.value
            | layerTexture.value
            | layerPainting.value
            | layerVideo.value;

        mask |= noPostLayerMask;
        return mask;
    }

    private int GetDoorLayerMask()
    {
        return layerDoor.value | namedDoorLayerMask;
    }

    private static bool IsInLayerMask(int layer, int mask)
    {
        return (mask & (1 << layer)) != 0;
    }

    private void ShowPrompt(bool isDoor)
    {
        if (UIIngameManager.Instance == null)
            return;

        UIIngameManager.Instance.ShowInteractPrompt(isDoor);
    }

    private void HidePrompt(bool isDoor)
    {
        if (UIIngameManager.Instance == null)
            return;

        UIIngameManager.Instance.HideInteractPrompt(isDoor);
    }


    private bool TryGetOffsetWorld(Collider col, bool isDoor, out Vector3 worldPos, out Transform targetTransform)
    {
        worldPos = col.bounds.center;
        targetTransform = null;

        if (!isDoor)
        {
            InteractObject item = col.GetComponent<InteractObject>();
            if (item == null)
            {
                item = col.GetComponentInParent<InteractObject>();
            }

            if (item != null)
            {
                targetTransform = item.transform;
                worldPos = item.transform.TransformPoint(item.EyeOffset);
                return true;
            }
        }
        else
        {
            DoorSceneLoader door = col.GetComponent<DoorSceneLoader>();
            if (door == null)
            {
                door = col.GetComponentInParent<DoorSceneLoader>();
            }

            if (door != null)
            {
                targetTransform = door.transform;
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
