using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovementComponent))]
[RequireComponent(typeof(PlayerStaminaComponent))]
[RequireComponent(typeof(PlayerGroundCheckComponent))]
public class PlayerSprintComponent : InputComponent
{
    public Action onStartSprintEvent;
    public Action onStopSprintEvent;
    [SerializeField] private float _sprintSpeedMultiplier = 1.5f;
    [SerializeField] private float _sprintStaminaRequirement = 20f;
    [SerializeField] private float _sprintStaminaCostPerSecond = 10f;
    [SerializeField] private float _staminaLockoutAfterSprint = 1f;
    [SerializeField] private InputActionReference _sprintInput;

    private Rigidbody _rigidbody;
    private PlayerMovementComponent _movementComponent;
    private PlayerStaminaComponent _staminaComponent;
    private PlayerGroundCheckComponent _groundCheckComponent;
    private bool _isTryingToSprint = false;
    private bool _isSprinting = false;
    private bool _consumingStamina = false;

    void Awake()
    {
        _movementComponent = GetComponent<PlayerMovementComponent>();
        _staminaComponent = GetComponent<PlayerStaminaComponent>();
        _groundCheckComponent = GetComponent<PlayerGroundCheckComponent>();
        _rigidbody = GetComponent<Rigidbody>();
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

    void Update()
    {
        if (!isLocalPlayer) return;

        CheckSprinting();

        if (_isSprinting)
        {
            if (!_staminaComponent.HasEnoughStamina(_sprintStaminaCostPerSecond * Time.deltaTime))
            {
                StopSprint();
                return;
            }
            if(Math.Abs(_rigidbody.linearVelocity.x) > 0.1f || Math.Abs(_rigidbody.linearVelocity.z) > 0.1f)
            {
                if (!_consumingStamina)
                {
                    DevLogger.Log("Player started moving while sprinting, disabling stamina regen.");
                    _staminaComponent.DisableStaminaRegen();
                    _consumingStamina = true;
                }
                _staminaComponent.ConsumeStamina(_sprintStaminaCostPerSecond * Time.deltaTime);
            }
            else if(_consumingStamina)
            {
                DevLogger.Log("Player stopped moving while sprinting, enabling stamina regen.");
                _consumingStamina = false;
                _staminaComponent.EnableStaminaRegen(_staminaLockoutAfterSprint);
            }
        }
    }

    void CheckSprinting()
    {
        if (_isTryingToSprint && !_isSprinting)
        {
             StartSprint();
        }
        else if (_isSprinting && !_isTryingToSprint)
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
        onStartSprintEvent?.Invoke();
    }

    void StopSprint()
    {
        if(!isLocalPlayer || !_isSprinting) return;

        _isSprinting = false;
        _movementComponent.MaxMoveSpeed /= _sprintSpeedMultiplier;
        _movementComponent.Acceleration /= _sprintSpeedMultiplier;
        _movementComponent.Deceleration /= _sprintSpeedMultiplier;
        onStopSprintEvent?.Invoke();

        if(_consumingStamina)
        {
            DevLogger.Log("Player stopped sprinting, enabling stamina regen.");
            _consumingStamina = false;
            _staminaComponent.EnableStaminaRegen(_staminaLockoutAfterSprint);
        }
    }
}