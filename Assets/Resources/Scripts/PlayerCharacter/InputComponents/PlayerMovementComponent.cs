using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerStaminaComponent))]
public class PlayerMovementComponent : InputComponent
{
    [SerializeField] private float _maxMoveSpeed = 5f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 15f;
    [SerializeField] private InputActionReference _movementInput;

    [Header("Step Climbing System")]
    [SerializeField] private float _maxStepHeight = 0.3f;
    [SerializeField] private float _stepSearchDistance = 0.5f;
    [SerializeField] private float _stepSmooth = 15f;
    [SerializeField] private float _playerRadius = 0.3f;
    [SerializeField] private float _centerToFeetOffset = 0.5f; //Player height
    [SerializeField] private LayerMask _stepLayer = ~0;

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
        HandleStepClimb();
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

        Vector3 currentHorizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        Vector3 newVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetVelocity, accelerationToUse * Time.fixedDeltaTime);
        newVelocity.y = currentVelocity.y; // Preserve vertical velocity (gravity/falling)

        _rigidbody.linearVelocity = newVelocity;
    }

    private void HandleStepClimb()
    {
        if (!_isMoving) return;
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();
        Vector3 targetForward = cameraForward * _movementDirection.z;
        Vector3 targetRight = Camera.main.transform.right * _movementDirection.x;
        Vector3 moveDir = (targetForward + targetRight).normalized;

        if (moveDir == Vector3.zero) return;
        Vector3 feetPos = transform.position - new Vector3(0f, _centerToFeetOffset, 0f);
        Vector3 lowerRayOrigin = feetPos + Vector3.up * 0.05f;

        // 3 ray
        Vector3[] rayOffsets = new Vector3[]
        {
            Vector3.zero,
            Vector3.Cross(moveDir, Vector3.up) * _playerRadius,
            -Vector3.Cross(moveDir, Vector3.up) * _playerRadius
        };

        bool hitStep = false;

        foreach (Vector3 offset in rayOffsets)
        {
            if (Physics.Raycast(lowerRayOrigin + offset, moveDir, out RaycastHit lowerHit, _stepSearchDistance, _stepLayer))
            {
                float surfaceAngle = Vector3.Angle(Vector3.up, lowerHit.normal); // Only steps if the obstacle has a hard angle
                if (surfaceAngle > 70f)
                {
                    hitStep = true;
                    break;
                }
            }
        }

        if (hitStep)
        {
            Vector3 upperRayOrigin = feetPos + Vector3.up * _maxStepHeight;
            if (!Physics.Raycast(upperRayOrigin, moveDir, _stepSearchDistance + 0.1f, _stepLayer))
            {
                Vector3 downRayOrigin = upperRayOrigin + (moveDir * (_stepSearchDistance + 0.1f));

                if (Physics.Raycast(downRayOrigin, Vector3.down, out RaycastHit downHit, _maxStepHeight, _stepLayer))
                {
                    float heightDifference = downHit.point.y - feetPos.y;
                    if (heightDifference > 0f)
                    {
                        Vector3 targetPos = _rigidbody.position + new Vector3(0f, heightDifference, 0f);
                        _rigidbody.position = Vector3.Lerp(_rigidbody.position, targetPos, Time.fixedDeltaTime * _stepSmooth);
                    }
                }
            }
        }
    }
}