using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovementComponent))]
[RequireComponent(typeof(PlayerStaminaComponent))]
[RequireComponent(typeof(PlayerGroundCheckComponent))]
public class PlayerSprintComponent : InputComponent
{
    [SerializeField] private float _sprintSpeedMultiplier = 1.5f;
    [SerializeField] private float _sprintStaminaRequirement = 20f;
    [SerializeField] private float _sprintStaminaCostPerSecond = 10f;
    [SerializeField] private float _staminaLockoutAfterSprint = 1f;
    [SerializeField] private InputActionReference _sprintInput;

    private PlayerMovementComponent _movementComponent;
    private PlayerStaminaComponent _staminaComponent;
    private PlayerGroundCheckComponent _groundCheckComponent;
    private bool _isTryingToSprint = false;
    private bool _isSprinting = false;

    void Awake()
    {
        _movementComponent = GetComponent<PlayerMovementComponent>();
        _staminaComponent = GetComponent<PlayerStaminaComponent>();
        _groundCheckComponent = GetComponent<PlayerGroundCheckComponent>();
    }

    protected override void BindInputs()
    {
        _sprintInput.action.Enable();
        _sprintInput.action.performed += OnStartSprinting;
        _sprintInput.action.canceled += OnStopSprinting;
    }

    protected override void UnbindInputs()
    {
        _sprintInput.action.Disable();
        _sprintInput.action.performed -= OnStartSprinting;
        _sprintInput.action.canceled -= OnStopSprinting;
    }

    void OnStartSprinting(InputAction.CallbackContext context)
    {
        _isTryingToSprint = true;
    }

    void OnStopSprinting(InputAction.CallbackContext context)
    {
        _isTryingToSprint = false;
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        CheckSprinting();

        if (_isSprinting)
        {
            _staminaComponent.ConsumeStamina(_sprintStaminaCostPerSecond * Time.fixedDeltaTime);
        }
    }

    void CheckSprinting()
    {
        if (_isTryingToSprint && _movementComponent.IsMoving && !_isSprinting)
        {
             StartSprint();
        }
        else if (_isSprinting && (!_isTryingToSprint || !_movementComponent.IsMoving))
        {
            StopSprint();
        }
    }

    void StartSprint()
    {
        if(!isLocalPlayer || _isSprinting || !_staminaComponent.HasEnoughStamina(_sprintStaminaRequirement) || !_groundCheckComponent.IsGrounded()) return;

        _isSprinting = true;
        _movementComponent.MaxMoveSpeed *= _sprintSpeedMultiplier;
        _movementComponent.Acceleration *= _sprintSpeedMultiplier;
        _movementComponent.Deceleration *= _sprintSpeedMultiplier;
        _staminaComponent.DisableStaminaRegen();
    }

    void StopSprint()
    {
        if(!isLocalPlayer || !_isSprinting) return;

        _isSprinting = false;
        _movementComponent.MaxMoveSpeed /= _sprintSpeedMultiplier;
        _movementComponent.Acceleration /= _sprintSpeedMultiplier;
        _movementComponent.Deceleration /= _sprintSpeedMultiplier;
        _staminaComponent.EnableStaminaRegen(_staminaLockoutAfterSprint);
    }
}