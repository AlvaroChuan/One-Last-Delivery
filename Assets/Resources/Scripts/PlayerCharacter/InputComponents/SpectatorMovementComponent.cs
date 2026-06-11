using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class SpectatorMovementComponent : InputComponent
{
    [SerializeField] private float _maxMoveSpeed = 5f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 15f;
    [SerializeField] private InputActionReference _movementInput;
    [SerializeField] private InputActionReference _ascendInput;
    [SerializeField] private InputActionReference _descendInput;
    private Rigidbody _rigidbody;
    private Collider _collider;
    private Vector3 _movementDirection;
    private bool _canMove = true;
    float _ascendValue = 0f;
    float _descendValue = 0f;
    Camera _camera;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _camera = GetComponentInChildren<Camera>();
    }

    protected override void OnEnable()
    {
        if (!isLocalPlayer)
        {
            enabled = false; // Disable this component for non-local players
            return;
        }
        DevLogger.Log("Enabling SpectatorMovementComponent for local player.");
        base.OnEnable();
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true; // Set collider to trigger to avoid physics interactions
        _rigidbody.useGravity = false; // Disable gravity for spectator movement
    }
    protected override void OnDisable()
    {
        if (!isLocalPlayer) return;
        base.OnDisable();
        if (_collider != null)
        {
            _collider.isTrigger = false; // Reset collider to non-trigger
        }
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false; // Reset rigidbody to non-kinematic
            _rigidbody.useGravity = true; // Re-enable gravity
        }
    }

    override protected void BindInputs()
    {
        _movementInput.action.Enable();
        _movementInput.action.performed += OnMovementInput;
        _movementInput.action.canceled += OnMovementInput;
        _ascendInput.action.Enable();
        _ascendInput.action.performed += OnAscendInput;
        _ascendInput.action.canceled += OnAscendInput;
        _descendInput.action.Enable();
        _descendInput.action.performed += OnDescendInput;
        _descendInput.action.canceled += OnDescendInput;
    }

    override protected void UnbindInputs()
    {
        _movementInput.action.Disable();
        _movementInput.action.performed -= OnMovementInput;
        _movementInput.action.canceled -= OnMovementInput;
        _ascendInput.action.Disable();
        _ascendInput.action.performed -= OnAscendInput;
        _ascendInput.action.canceled -= OnAscendInput;
        _descendInput.action.Disable();
        _descendInput.action.performed -= OnDescendInput;
        _descendInput.action.canceled -= OnDescendInput;
    }

    void OnMovementInput(InputAction.CallbackContext context)
    {
        Vector2 inputVector = context.ReadValue<Vector2>();
        _movementDirection = new Vector3(inputVector.x, 0f, inputVector.y);
    }

    void OnAscendInput(InputAction.CallbackContext context)
    {
        _ascendValue = context.ReadValue<float>();
    }

    void OnDescendInput(InputAction.CallbackContext context)
    {
        _descendValue = context.ReadValue<float>();
    }

    public void SetMovementDirection(Vector3 direction)
    {
        if (!isLocalPlayer) return; // Only allow local player to set movement direction

        _movementDirection = direction.normalized;
    }

    void FixedUpdate()
    {
        DevLogger.Log("Movement Action enabled: " + _movementInput.action.enabled);

        if(_rigidbody == null || !_canMove || !isLocalPlayer)
            return;

        HandleMovement();
    }
    private void HandleMovement()
    {
        Vector3 currentVelocity = _rigidbody.linearVelocity;
        Vector3 targetFoward = _camera.transform.forward * _movementDirection.z;
        Vector3 targetRight = _camera.transform.right * _movementDirection.x;
        Vector3 targetUp = Vector3.up * (_ascendValue - _descendValue);
        Vector3 targetVelocity = (targetFoward + targetRight + targetUp).normalized * _maxMoveSpeed;

        float accelerationToUse = Vector3.Dot(currentVelocity, targetVelocity) <= 0f ? _deceleration : _acceleration;

        Vector3 newVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, accelerationToUse * Time.fixedDeltaTime);

        _rigidbody.linearVelocity = newVelocity;
    }
}