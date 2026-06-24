using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.EventSystems;
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
    [SerializeField] private bool maintainEditorPointerLock;
    [SerializeField] private bool simulateWebTemplateInEditor = true;
    [SerializeField] private bool enableEditorKeyboardFallback = true;
    [SerializeField] private bool enableEditorMouseLookFallback = true;

    Vector2 moveInput;
    Vector2 lookInput;

    [SerializeField] public float moveSpeed = 5f;
    [SerializeField, Range(0.1f, 50f)] private float acceleration = 30f;
    [SerializeField] private float mouseSensitivity = 25f;

    [Header("Camara")]
    [SerializeField] private CinemachineVirtualCameraBase virtualCamera;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Camera playerCamera;

    [Header("Configuracion de Camara")]
    public float cameraFocusDuration = 0.5f;
    private Coroutine cameraMoveCoroutine;

    private CharacterController controller;
    private Transform cameraHolder;
    private Transform defaultCameraParent;
    private Vector3 defaultCameraLocalPosition;
    private Quaternion defaultCameraLocalRotation;
    private bool defaultCameraPoseCached;
    private float xRotation = 0f;

    public bool isInteracting;

    private Vector3 currentVelocity;

    private bool isMobile;
    private bool appliedInteractingState;

    protected CharacterController MovementController
    {
        get
        {
            if (controller == null)
                controller = GetComponent<CharacterController>();

            return controller;
        }
    }

    protected float MovementAcceleration => acceleration;

    protected virtual void Start()
    {
        EnsureReferences();
        isMobile = DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile;

        ApplyInteractionState(isInteracting);
    }

    protected virtual void Update()
    {
        SyncExternalInteractionState();
        HandleEditorTemplateInput();

        bool pauseGameplayForWebFocus = ShouldPauseGameplayForWebFocus();
        if (pauseGameplayForWebFocus)
        {
            PauseGameplayUntilCanvasFocus();
            ApplyGrounding();
            return;
        }

        bool allowEditorMovementWhileInteracting = ShouldAllowEditorKeyboardMovement();
        bool allowEditorLookWhileInteracting = ShouldAllowEditorMouseLook();

        if (isInteracting && !allowEditorMovementWhileInteracting && !allowEditorLookWhileInteracting)
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
            if (!isInteracting || allowEditorMovementWhileInteracting)
            {
                HandleMovement();
            }

            if (!usePointerLook)
            {
                HandleMouseLook();
            }
        }

        ApplyGrounding();
    }

    protected virtual void Awake()
    {
        isInteracting = false;
        appliedInteractingState = false;
        inputActions = new InputSystem_Actions();
    }

    protected virtual void OnEnable()
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

    protected virtual void OnDisable()
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

    private bool ShouldPauseGameplayForWebFocus()
    {
        return !isMobile
            && !isInteracting
            && !usePointerLook
            && !BenignoGLWebBridge.IsGameplayFocused();
    }

    private void PauseGameplayUntilCanvasFocus()
    {
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        currentVelocity = Vector3.zero;

        if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        if (ShouldPauseGameplayForWebFocus())
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

    public virtual void TeleportTo(Transform target)
    {
        if (target == null) return;

        Quaternion bodyRotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        TeleportTo(target.position, bodyRotation, target.eulerAngles.x);
    }

    public virtual void TeleportTo(Vector3 targetPosition, Quaternion targetRotation, float cameraPitch)
    {
        EnsureReferences();

        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controller != null)
            controller.enabled = false;

        transform.SetPositionAndRotation(targetPosition, targetRotation);

        SetCameraPitch(cameraPitch);
        ResetMovementState();

        if (controller != null)
            controller.enabled = controllerWasEnabled;
    }

    public virtual void ResetMovementState()
    {
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        currentVelocity = Vector3.zero;
    }

    private void EnsureReferences()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (virtualCamera == null)
            virtualCamera = GetComponentInChildren<CinemachineVirtualCameraBase>(true);

        if (cameraPivot == null && virtualCamera != null)
            cameraPivot = virtualCamera.transform;

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (cameraPivot == null && playerCamera != null)
        {
            Transform cameraParent = playerCamera.transform.parent;
            cameraPivot = cameraParent != null && cameraParent.IsChildOf(transform)
                ? cameraParent
                : playerCamera.transform;
        }

        if (cameraHolder == null && cameraPivot != null)
            cameraHolder = cameraPivot;

        CacheDefaultCameraPose();
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
        if (targetPivot == null)
            yield break;

        EnsureReferences();
        SetVirtualCameraEnabled(false);

        Transform cameraTransform = GetControlledCameraTransform();
        if (cameraTransform == null)
        {
            SetVirtualCameraEnabled(true);
            yield break;
        }

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

        cameraTransform.SetParent(targetPivot, true);
    }

    public void ReturnCamera()
    {
        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(cameraMoveCoroutine);
            cameraMoveCoroutine = null;
        }

        EnsureReferences();

        Transform cameraTransform = GetControlledCameraTransform();
        if (cameraTransform != null && defaultCameraPoseCached)
        {
            cameraTransform.SetParent(defaultCameraParent, false);
            cameraTransform.localPosition = defaultCameraLocalPosition;
            cameraTransform.localRotation = defaultCameraLocalRotation;
        }

        SetCameraPitch(xRotation);
        SetVirtualCameraEnabled(true);
        SetInteracting(false);
    }

    private Transform GetControlledCameraTransform()
    {
        if (playerCamera != null)
            return playerCamera.transform;

        Camera mainCamera = Camera.main;
        return mainCamera != null ? mainCamera.transform : null;
    }

    private void CacheDefaultCameraPose()
    {
        if (defaultCameraPoseCached || playerCamera == null)
            return;

        Transform cameraTransform = playerCamera.transform;
        defaultCameraParent = cameraTransform.parent;
        defaultCameraLocalPosition = cameraTransform.localPosition;
        defaultCameraLocalRotation = cameraTransform.localRotation;
        defaultCameraPoseCached = true;
    }

    private void SetVirtualCameraEnabled(bool value)
    {
        if (virtualCamera != null)
            virtualCamera.enabled = value;
    }

    protected virtual void HandleMovement()
    {
        Vector2 effectiveMoveInput = GetEffectiveMoveInput();
        Vector3 moveDirection = (transform.forward * effectiveMoveInput.y + transform.right * effectiveMoveInput.x).normalized;
        Vector3 desiredVelocity = moveDirection * moveSpeed;
        currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, acceleration * Time.deltaTime);
        controller.Move(currentVelocity * Time.deltaTime);
    }

    protected Vector2 GetEffectiveMoveInput()
    {
#if UNITY_EDITOR
        if (enableEditorKeyboardFallback)
        {
            Vector2 keyboardInput = ReadEditorKeyboardMoveInput();
            if (keyboardInput.sqrMagnitude > 0f)
                return keyboardInput;
        }
#endif

        return moveInput;
    }

    private bool ShouldAllowEditorKeyboardMovement()
    {
#if UNITY_EDITOR
        return enableEditorKeyboardFallback && ReadEditorKeyboardMoveInput().sqrMagnitude > 0f;
#else
        return false;
#endif
    }

    protected virtual void ApplyGrounding()
    {
        if (MovementController != null && MovementController.enabled)
            MovementController.Move(new Vector3(0, -0.1f, 0));
    }

#if UNITY_EDITOR
    private Vector2 ReadEditorKeyboardMoveInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                input.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                input.y -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                input.x += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                input.x -= 1f;
        }

        return Vector2.ClampMagnitude(input, 1f);
    }
#endif

    private void HandleMouseLook()
    {
        if (usePointerLook) return;

        EnsureReferences();
        if (cameraHolder == null)
            return;

        Vector2 effectiveLookInput = GetEffectiveLookInput();
        float mouseX = effectiveLookInput.x * mouseSensitivity;
        float mouseY = effectiveLookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private Vector2 GetEffectiveLookInput()
    {
#if UNITY_EDITOR
        if (ShouldAllowEditorMouseLook())
        {
            Vector2 mouseDelta = ReadEditorMouseLookInput();
            if (mouseDelta.sqrMagnitude > 0f)
                return mouseDelta;
        }
#endif

        return lookInput;
    }

    private bool ShouldAllowEditorMouseLook()
    {
#if UNITY_EDITOR
        return enableEditorMouseLookFallback
            && !isMobile
            && !usePointerLook
            && !IsPointerOverUI();
#else
        return false;
#endif
    }

#if UNITY_EDITOR
    private Vector2 ReadEditorMouseLookInput()
    {
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            if (delta.sqrMagnitude > 0f)
                return delta;
        }
        return Vector2.zero;
    }
#endif

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

    private void HandleEditorTemplateInput()
    {
#if UNITY_EDITOR
        if (!simulateWebTemplateInEditor || isMobile || isInteracting || usePointerLook)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            lookInput = Vector2.zero;
            return;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (IsPointerOverUI())
            return;

        ApplyPointerMode();
#endif
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void HandleKeyLook()
    {
        if (Keyboard.current == null)
            return;

        EnsureReferences();
        if (cameraHolder == null)
            return;

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
