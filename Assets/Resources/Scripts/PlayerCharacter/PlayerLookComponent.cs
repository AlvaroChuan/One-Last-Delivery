using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;


public class PlayerLookComponent : PlayerComponent
{
    [SerializeField] private GameObject _model;
    [SerializeField] private GameObject _head;
    [SerializeField] private Transform _eyes;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private string[] _cameraTags = new string[] { "PlayerCamera", "SpectatorCamera" };
    [SerializeField] private string _mainCameraTag = "PlayerCamera";
    private List<CinemachineCamera> _cinemachineCameras = new List<CinemachineCamera>();
    CinemachineCamera _currentCamera;
    public GameObject Model => _model;
    public Transform Eyes => _eyes;
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isLocalPlayer)
        {
            enabled = false; // Disable this component for non-local players
            return;
        }

        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (var camera in cameras)
        {
            if (System.Array.Exists(_cameraTags, tag => camera.CompareTag(tag)))
            {
                _cinemachineCameras.Add(camera);
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SwitchCamera(_mainCameraTag); // Set the main camera as the active camera
    }

    void OnEnable()
    {
        if (!isLocalPlayer) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_currentCamera != null)
        {
            _currentCamera.enabled = true;
            _currentCamera.Follow = _eyes;
            _currentCamera.LookAt = _eyes;
        }
    }

    void OnDisable()
    {
        if (!isLocalPlayer) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_currentCamera != null)
        {
            _currentCamera.enabled = false;
            _currentCamera.Follow = null;
            _currentCamera.LookAt = null;
        }
    }

    public void SwitchCamera(string cameraTag)
    {
        if (!isLocalPlayer) return;

        foreach (var camera in _cinemachineCameras)
        {
            if (camera.CompareTag(cameraTag))
            {
                _currentCamera = camera;
                camera.enabled = true;
                camera.Follow = _eyes;
                camera.LookAt = _eyes;
            }
            else
            {
                camera.enabled = false;
                camera.Follow = null;
                camera.LookAt = null;
            }
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