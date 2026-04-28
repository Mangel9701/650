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
    private bool usePointerLook = true;

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

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraHolder = virtualCamera.transform;
        isMobile = DeviceDetector.Instance.IsMobile;

        ApplyPointerMode();
    }

    void Update()
    {
        if (!isInteracting)
        {
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

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void MoveCameraToTarget(Transform targetPivot)
    {
        isInteracting = true;

        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(cameraMoveCoroutine);
        }

        cameraMoveCoroutine = StartCoroutine(SmoothCameraMove(targetPivot));
    }

    public void SetUsePointerLook(bool value)
    {
        usePointerLook = value;
        ApplyPointerMode();
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
        isInteracting = false;
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
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            lookInput = Vector2.zero;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
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
