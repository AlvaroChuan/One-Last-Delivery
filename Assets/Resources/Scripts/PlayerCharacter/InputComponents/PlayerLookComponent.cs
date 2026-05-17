using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLookComponent : InputComponent
{
    [SerializeField] private float _sensitivity = 10f;
    [SerializeField] private float _maxVerticalAngle = 80f;
    [SerializeField] private Transform _cameraHorizontalPivot;
    [SerializeField] private Transform _cameraVerticalPivot;
    [SerializeField] private InputActionReference _lookInput;

    protected override void Start()
    {
        base.Start();
        if (!isLocalPlayer)
        {
            Camera playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(false); // Disable camera for non-local players
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
        Vector2 lookInput = context.ReadValue<Vector2>();
        Look(lookInput);
    }

    public void Look(Vector2 lookInput)
    {
        if (!isLocalPlayer) return; // Only allow local player to process look input

        // Apply horizontal rotation to the horizontal pivot (yaw)
        float currentYaw = _cameraHorizontalPivot.localEulerAngles.y;
        float targetYaw = currentYaw + lookInput.x * _sensitivity * Time.deltaTime;
        _cameraHorizontalPivot.localRotation = Quaternion.Euler(0f, targetYaw, 0f);
        // Apply vertical rotation to the vertical pivot (pitch) with clamping
        float currentPitch = _cameraVerticalPivot.localEulerAngles.x;
        currentPitch = (currentPitch > 180f) ? currentPitch - 360f : currentPitch; // Convert to -180 to 180 range
        float targetPitch = Mathf.Clamp(currentPitch - lookInput.y * _sensitivity * Time.deltaTime, -_maxVerticalAngle, _maxVerticalAngle);
        _cameraVerticalPivot.localRotation = Quaternion.Euler(targetPitch, 0f, 0f);
    }
}