using UnityEngine;
using Mirror;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationComponent : MonoBehaviour
{
    [SerializeField] private Hitbox _weaponHitbox;
    private Animator _animator;
    private PlayerMovementComponent _movementComponent;
    private PlayerSprintComponent _sprintComponent;
    private PlayerJumpComponent _jumpComponent;
    private PlayerGroundCheckComponent _groundCheckComponent;
    private PlayerInventoryComponent _inventoryComponent;
    private PlayerItemUseComponent _itemUseComponent;

    private readonly int _isWalkingHash = Animator.StringToHash("IsWalking");
    private readonly int _isRunningHash = Animator.StringToHash("IsRunning");
    private readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int _jumpHash = Animator.StringToHash("Jump");
    private readonly int _attackHash = Animator.StringToHash("BatAttack");
    private readonly int _holdTypeHash = Animator.StringToHash("HoldType");
    private readonly int _sittingTypeHash = Animator.StringToHash("SittingType");

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _movementComponent = GetComponent<PlayerMovementComponent>();
        _sprintComponent = GetComponent<PlayerSprintComponent>();
        _jumpComponent = GetComponent<PlayerJumpComponent>();
        _groundCheckComponent = GetComponent<PlayerGroundCheckComponent>();
        _inventoryComponent = GetComponent<PlayerInventoryComponent>();
        _itemUseComponent = GetComponent<PlayerItemUseComponent>();
    }

    void Start()
    {
        if (_animator.layerCount > 1) _animator.SetLayerWeight(1, 1f);
    }

    void OnEnable()
    {
        if (_jumpComponent != null) _jumpComponent.onJumpEvent += HandleJump;
        if (_itemUseComponent != null) _itemUseComponent.onItemUseStart += HandleItemUse;
        if (_inventoryComponent != null) _inventoryComponent.onInventorySlotChangedOwner += HandleInventoryChange;
    }

    void OnDisable()
    {
        if (_jumpComponent != null) _jumpComponent.onJumpEvent -= HandleJump;
        if (_itemUseComponent != null) _itemUseComponent.onItemUseStart -= HandleItemUse;
        if (_inventoryComponent != null) _inventoryComponent.onInventorySlotChangedOwner -= HandleInventoryChange;
    }

    void Update()
    {
        UpdateLocomotion();
        UpdateGroundedState();
    }

    private void UpdateLocomotion()
    {
        if (_movementComponent == null) return;
        bool isSprinting = _sprintComponent != null && _sprintComponent.IsSprinting;
        _animator.SetBool(_isWalkingHash, _movementComponent.IsMoving && !isSprinting);
        _animator.SetBool(_isRunningHash, isSprinting);
    }

    private void UpdateGroundedState()
    {
        if (_groundCheckComponent != null) _animator.SetBool(_isGroundedHash, _groundCheckComponent.IsGrounded());
    }

    public void SetSittingState(bool isDriving)
    {
        _animator.SetInteger(_sittingTypeHash, isDriving ? 2 : 1);
    }

    public void ResetToNormalState()
    {
        _animator.SetInteger(_sittingTypeHash, 0);
    }

    private void HandleJump(PlayerJumpComponent.JumpInfo info)
    {
        if (info.isSuccessful) _animator.SetTrigger(_jumpHash);
    }

    private void HandleItemUse(ItemID itemID)
    {
        if (itemID == ItemID.BaseballBat) _animator.SetTrigger(_attackHash);
    }

    private void HandleInventoryChange(PlayerInventoryComponent.SlotChangeInfo info)
    {
        if (info.newSlotIndex == -1 || info.newItemData.itemID == ItemID.None)
        {
            _animator.SetInteger(_holdTypeHash, 0);
        }
        else
        {
            switch (info.newItemData.itemID)
            {
                case ItemID.Map: _animator.SetInteger(_holdTypeHash, 2); break;
                case ItemID.BaseballBat:
                case ItemID.Taser:
                case ItemID.Torch:
                case ItemID.WalkieTalkie: _animator.SetInteger(_holdTypeHash, 1); break;
                default: _animator.SetInteger(_holdTypeHash, 0); break;
            }
        }
    }

    public void EnableHitbox()
    {
        if (_weaponHitbox != null)
        {
            _weaponHitbox.EnableHitbox();
        }
        else
        {
            Debug.LogWarning("PlayerAnimationComponent: no weapon Hitbox assigned.");
        }
    }

    public void DisableHitbox()
    {
        if (_weaponHitbox != null)
        {
            _weaponHitbox.DisableHitbox();
        }
    }
}