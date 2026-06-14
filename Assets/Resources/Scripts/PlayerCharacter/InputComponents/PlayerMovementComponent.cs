using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerStaminaComponent))]
public class PlayerMovementComponent : InputComponent
{
    [SerializeField] private float _maxMoveSpeed = 5f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 15f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private InputActionReference _movementInput;
    [SerializeField] private GameObject _model;

    public float MaxMoveSpeed {
        get => _maxMoveSpeed;
        set => _maxMoveSpeed = value;
    }
    public float Acceleration {
        get => _acceleration;
        set => _acceleration = value;
    }
    public float Deceleration {
        get => _deceleration;
        set => _deceleration = value;
    }
    public bool IsMoving => _isMoving;
    private Rigidbody _rigidbody;
    private Vector3 _movementDirection;
    private bool _canMove = true;
    private bool _isMoving = false;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isLocalPlayer)
        {
            _rigidbody.isKinematic = true;
            return;
        }
    }

    protected override void BindInputs()
    {
        _movementInput.action.Enable();
        _movementInput.action.performed += OnMovementInput;
        _movementInput.action.canceled += OnMovementInput;
    }
    protected override void UnbindInputs()
    {
        _movementInput.action.Disable();
        _movementInput.action.performed -= OnMovementInput;
        _movementInput.action.canceled -= OnMovementInput;
    }

    void OnMovementInput(InputAction.CallbackContext context)
    {
        // Extra safeguard: Double check network authority before processing input
        if (!isLocalPlayer) return;

        Vector2 inputVector = context.ReadValue<Vector2>();
        _movementDirection = new Vector3(inputVector.x, 0f, inputVector.y);

        _isMoving = _movementDirection != Vector3.zero;
    }

    public void SetMovementDirection(Vector3 direction)
    {
        if (!isLocalPlayer) return; // Only allow local player to set movement direction

        _movementDirection = direction.normalized;
    }

    void FixedUpdate()
    {
        if(_rigidbody == null || !_canMove || !isLocalPlayer)
            return;

        HandleMovement();
        HandleRotation();
    }
    private void HandleMovement()
    {
        Vector3 currentVelocity = _rigidbody.linearVelocity;
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f; // Flatten the camera forward vector to the horizontal plane
        cameraForward.Normalize();
        Vector3 targetFoward = cameraForward * _movementDirection.z;
        Vector3 targetRight = Camera.main.transform.right * _movementDirection.x;
        Vector3 targetVelocity = (targetFoward + targetRight) * _maxMoveSpeed;

        float accelerationToUse = Vector3.Dot(currentVelocity, targetVelocity) <= 0f ? _deceleration : _acceleration;

        Vector3 newVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, accelerationToUse * Time.fixedDeltaTime);
        newVelocity.y = currentVelocity.y; // Preserve vertical velocity (gravity/falling)

        _rigidbody.linearVelocity = newVelocity;
    }

    private void HandleRotation()
    {
        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward);
            _model.transform.rotation = Quaternion.Slerp(_model.transform.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime);
        }
    }
}