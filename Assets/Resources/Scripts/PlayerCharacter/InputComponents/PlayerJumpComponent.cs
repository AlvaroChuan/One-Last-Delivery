using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerStaminaComponent))]
[RequireComponent(typeof(PlayerGroundCheckComponent))]
public class PlayerJumpComponent : InputComponent
{
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _jumpStaminaCost = 10f;
    [SerializeField] private float _staminaLockoutDuration = 1f;
    [SerializeField] private InputActionReference _jumpInput;
    private Rigidbody _rigidbody;
    private PlayerStaminaComponent _staminaComponent;
    private PlayerGroundCheckComponent _groundCheckComponent;
    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _staminaComponent = GetComponent<PlayerStaminaComponent>();
        _groundCheckComponent = GetComponent<PlayerGroundCheckComponent>();
    }

    protected override void BindInputs()
    {
        _jumpInput.action.Enable();
        _jumpInput.action.performed += OnJump;
    }

    protected override void UnbindInputs()
    {
        _jumpInput.action.Disable();
        _jumpInput.action.performed -= OnJump;
    }

    void OnJump(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        if (!_groundCheckComponent.IsGrounded()) return; // Only allow jumping when grounded

        if (_staminaComponent.HasEnoughStamina(_jumpStaminaCost))
        {
            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            _staminaComponent.ConsumeStamina(_jumpStaminaCost);
            _staminaComponent.DisableStaminaRegen(_staminaLockoutDuration);
        }
    }
}