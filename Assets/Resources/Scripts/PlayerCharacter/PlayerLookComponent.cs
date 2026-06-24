using UnityEngine;
using Unity.Cinemachine;


public class PlayerLookComponent : PlayerComponent
{
    [SerializeField] private GameObject _model;
    [SerializeField] private GameObject _head;
    [SerializeField] private Transform _eyes;
    [SerializeField] private float _rotationSpeed = 10f;
    public GameObject Model => _model;
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isLocalPlayer)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            CinemachineCamera cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();

            if (cinemachineCamera != null)
            {
                cinemachineCamera.Follow = _eyes;
                cinemachineCamera.LookAt = _eyes;
            }
        }
        else
        {
            enabled = false; // Disable this component for non-local players
        }
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return; // Only allow local player to process look input

        HandleRotation();
    }

    private void HandleRotation()
    {
        Vector3 forward = Camera.main.transform.forward;
        Vector3 flatForward = new Vector3(forward.x, 0f, forward.z).normalized;

        if (flatForward.sqrMagnitude > 0.001f)
        {
            Quaternion modelTargetRotation = Quaternion.LookRotation(flatForward);
            _model.transform.rotation = Quaternion.Slerp(_model.transform.rotation, modelTargetRotation, _rotationSpeed * Time.fixedDeltaTime);
        }
        Quaternion headTargetRotation = Quaternion.LookRotation(forward);
        _head.transform.rotation = Quaternion.Slerp(_head.transform.rotation, headTargetRotation, _rotationSpeed * Time.fixedDeltaTime);
    }
}

/*

public class PlayerLookComponent : InputComponent
{
    [SerializeField] private float _sensitivity = 10f;
    [SerializeField] private float _maxVerticalAngle = 80f;
    [SerializeField] private Transform _cameraHorizontalPivot;
    [SerializeField] private Transform _cameraVerticalPivot;
    [SerializeField] private InputActionReference _lookInput;
    Camera _playerCamera;
    public Camera PlayerCamera => _playerCamera;
    Vector2 _currentLookInput;

    public override void OnStartClient()
    {
        base.OnStartClient();
        _playerCamera = GetComponentInChildren<Camera>();
        if (!isLocalPlayer)
        {
            if (_playerCamera != null)
            {
                _playerCamera.gameObject.SetActive(false); // Disable camera for non-local players
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    protected override void BindInputs()
    {
        _lookInput.action.Enable();
        _lookInput.action.performed += OnLookInput;
        _lookInput.action.canceled += OnLookInput;
    }

    protected override void UnbindInputs()
    {
        _lookInput.action.Disable();
        _lookInput.action.performed -= OnLookInput;
        _lookInput.action.canceled -= OnLookInput;
    }

    void OnLookInput(InputAction.CallbackContext context)
    {
        _currentLookInput = context.ReadValue<Vector2>();
    }

    public void Look()
    {
        if (!isLocalPlayer) return; // Only allow local player to process look input

        // Apply horizontal rotation to the horizontal pivot (yaw)
        float currentYaw = _cameraHorizontalPivot.localEulerAngles.y;
        float targetYaw = currentYaw + _currentLookInput.x * _sensitivity * Time.deltaTime;
        _cameraHorizontalPivot.localRotation = Quaternion.Euler(0f, targetYaw, 0f);
        // Apply vertical rotation to the vertical pivot (pitch) with clamping
        float currentPitch = _cameraVerticalPivot.localEulerAngles.x;
        currentPitch = (currentPitch > 180f) ? currentPitch - 360f : currentPitch; // Convert to -180 to 180 range
        float targetPitch = Mathf.Clamp(currentPitch - _currentLookInput.y * _sensitivity * Time.deltaTime, -_maxVerticalAngle, _maxVerticalAngle);
        _cameraVerticalPivot.localRotation = Quaternion.Euler(targetPitch, 0f, 0f);
    }

    void LateUpdate()
    {
        Look();
    }
}
*/