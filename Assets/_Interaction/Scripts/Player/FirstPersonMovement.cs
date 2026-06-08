using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonMovement : MonoBehaviour
{
    [Header("Controles alternativos")]
    public float alternateLookSpeed = 100f;

    [Header("Valores de control")]
    public InputSystem_Actions inputActions;

    [Header("Opciones de entrada")]
    [SerializeField]
    private bool usePointerLook;
    [SerializeField] private bool maintainEditorPointerLock = true;

    Vector2 moveInput;
    Vector2 lookInput;

    [SerializeField] public float moveSpeed = 5f;
    [SerializeField, Range(0.1f, 50f)] private float acceleration = 30f;
    [SerializeField] private float mouseSensitivity = 25f;

    [Header("Camara")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;

    [Header("Configuracion de Camara")]
    public float cameraFocusDuration = 0.5f;
    private Coroutine cameraMoveCoroutine;

    private CharacterController controller;
    private Transform cameraHolder;
    private float xRotation = 0f;

    public bool isInteracting;

    private Vector3 currentVelocity;

    private bool isMobile;
    private bool appliedInteractingState;

    void Start()
    {
        EnsureReferences();
        isMobile = DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile;

        ApplyInteractionState(isInteracting);
    }

    void Update()
    {
        SyncExternalInteractionState();

        if (ShouldPauseForWebFocus())
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            currentVelocity = Vector3.zero;

            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            controller.Move(new Vector3(0, -0.1f, 0));
            return;
        }

        if (isInteracting)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                BenignoGLWebBridge.SetGameplayPointerMode(false);
            }
        }
        else
        {
            EnsureEditorPointerLock();
            HandleMovement();

            if (!usePointerLook)
            {
                HandleMouseLook();
            }
        }

        controller.Move(new Vector3(0, -0.1f, 0));
    }

    private void Awake()
    {
        isInteracting = false;
        appliedInteractingState = false;
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;

        if (isMobile)
        {
            // En dispositivos moviles, podrías usar un joystick virtual para el movimiento y la cam, por lo que no necesitas suscribirte a eventos de look.
        }
        else
        {
            inputActions.Player.Look.performed += OnLook;
            inputActions.Player.Look.canceled += OnLook;
        }
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;

        if (!isMobile)
        {
            inputActions.Player.Look.performed -= OnLook;
            inputActions.Player.Look.canceled -= OnLook;
        }

        inputActions.Player.Disable();
    }

    public void LockCursorFromUserGesture()
    {
        if (!isMobile && !isInteracting)
        {
            ApplyPointerMode();
        }
    }

    private bool ShouldPauseForWebFocus()
    {
        return !isMobile
            && !isInteracting
            && !usePointerLook
            && !BenignoGLWebBridge.IsGameplayFocused();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (ShouldPauseForWebFocus())
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        if (ShouldPauseForWebFocus())
        {
            lookInput = Vector2.zero;
            return;
        }

        lookInput = context.ReadValue<Vector2>();
    }

    public void MoveCameraToTarget(Transform targetPivot)
    {
        SetInteracting(true);

        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(cameraMoveCoroutine);
        }

        cameraMoveCoroutine = StartCoroutine(SmoothCameraMove(targetPivot));
    }

    public void SetUsePointerLook(bool value)
    {
        usePointerLook = value;

        if (isInteracting)
            ApplyInteractionCursorMode();
        else
            ApplyPointerMode();
    }

    public void SetInteracting(bool value)
    {
        if (isInteracting == value && appliedInteractingState == value)
            return;

        isInteracting = value;
        ApplyInteractionState(value);
    }

    public void TeleportTo(Transform target)
    {
        if (target == null) return;

        EnsureReferences();

        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controller != null)
            controller.enabled = false;

        Quaternion bodyRotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        transform.SetPositionAndRotation(target.position, bodyRotation);

        SetCameraPitch(target.eulerAngles.x);
        ResetMovementState();

        if (controller != null)
            controller.enabled = controllerWasEnabled;
    }

    public void ResetMovementState()
    {
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        currentVelocity = Vector3.zero;
    }

    private void EnsureReferences()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (cameraHolder == null && virtualCamera != null)
            cameraHolder = virtualCamera.transform;
    }

    private void SetCameraPitch(float pitch)
    {
        xRotation = NormalizePitch(pitch);

        if (cameraHolder != null)
            cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private float NormalizePitch(float pitch)
    {
        if (pitch > 180f)
            pitch -= 360f;

        return Mathf.Clamp(pitch, -90f, 90f);
    }

    private IEnumerator SmoothCameraMove(Transform targetPivot)
    {
        CinemachineVirtualCamera brain = virtualCamera.GetComponent<CinemachineVirtualCamera>();

        if (brain != null)
        {
            brain.enabled = false;
        }

        if (Camera.main == null) yield break;

        Transform cameraTransform = Camera.main.transform;

        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        float elapsed = 0f;

        while (elapsed < cameraFocusDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cameraFocusDuration;

            float tSmooth = Mathf.SmoothStep(0f, 1f, t);

            cameraTransform.position = Vector3.Lerp(startPos, targetPivot.position, tSmooth);
            cameraTransform.rotation = Quaternion.Slerp(startRot, targetPivot.rotation, tSmooth);

            yield return null;
        }

        cameraTransform.position = targetPivot.position;
        cameraTransform.rotation = targetPivot.rotation;

        cameraTransform.SetParent(targetPivot);
    }

    public void ReturnCamera()
    {
        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(cameraMoveCoroutine);
        }

        CinemachineVirtualCamera brain = virtualCamera.GetComponent<CinemachineVirtualCamera>();

        if (brain != null)
        {
            brain.enabled = true;
        }

        SetInteracting(false);
    }

    private void HandleMovement()
    {
        Vector3 moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        Vector3 desiredVelocity = moveDirection * moveSpeed;
        currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, acceleration * Time.deltaTime);
        controller.Move(currentVelocity * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        if (usePointerLook) return;

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void ApplyPointerMode()
    {
        if (isMobile) return;

        if (usePointerLook)
        {
            BenignoGLWebBridge.SetGameplayPointerMode(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            lookInput = Vector2.zero;
        }
        else
        {
            BenignoGLWebBridge.SetGameplayPointerMode(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void SyncExternalInteractionState()
    {
        if (appliedInteractingState == isInteracting)
            return;

        ApplyInteractionState(isInteracting);
    }

    private void ApplyInteractionState(bool value)
    {
        appliedInteractingState = value;
        ResetMovementState();

        if (value)
            ApplyInteractionCursorMode();
        else
            ApplyPointerMode();
    }

    private void ApplyInteractionCursorMode()
    {
        if (isMobile) return;

        BenignoGLWebBridge.SetGameplayPointerMode(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void EnsureEditorPointerLock()
    {
#if UNITY_EDITOR
        if (!maintainEditorPointerLock || isMobile || usePointerLook)
            return;

        if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)
            ApplyPointerMode();
#endif
    }

    private void HandleKeyLook()
    {
        float keyX = 0f;
        float keyY = 0f;

        if (Keyboard.current.iKey.isPressed) keyY = 1f;
        if (Keyboard.current.kKey.isPressed) keyY = -1f;
        if (Keyboard.current.jKey.isPressed) keyX = -1f;
        if (Keyboard.current.lKey.isPressed) keyX = 1f;

        float mouseX = keyX * alternateLookSpeed * Time.deltaTime;
        float mouseY = keyY * alternateLookSpeed * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    public void SetLookInput(Vector2 input)
    {
        lookInput = input;
    }

    public void SetSensibility(float speed)
    {
        mouseSensitivity = speed;
    }
}
