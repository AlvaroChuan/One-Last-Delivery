using UnityEngine;
using Mirror;
using Mirror.Examples.Basic;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationComponent : PlayerComponent
{
    [SerializeField] private Hitbox _weaponHitbox;
    [SerializeField] private AimIKTarget _aimIKTarget;
    private Animator _animator;
    private NetworkAnimator _networkAnimator;
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

    private float _targetLayer1Weight = 0f;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _networkAnimator = GetComponent<NetworkAnimator>();
        _movementComponent = GetComponent<PlayerMovementComponent>();
        _sprintComponent = GetComponent<PlayerSprintComponent>();
        _jumpComponent = GetComponent<PlayerJumpComponent>();
        _groundCheckComponent = GetComponent<PlayerGroundCheckComponent>();
        _inventoryComponent = GetComponent<PlayerInventoryComponent>();
        _itemUseComponent = GetComponent<PlayerItemUseComponent>();
    }

    void Start()
    {
        if (_animator.layerCount > 1)
        {
            int currentHoldType = _animator.GetInteger(_holdTypeHash);
            _targetLayer1Weight = currentHoldType != 0 ? 1f : 0f;
            _animator.SetLayerWeight(1, _targetLayer1Weight);
        }
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
        UpdateLayerWeights();
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

    private void UpdateLayerWeights()
    {
        if (_animator.layerCount > 1)
        {
            float currentWeight = _animator.GetLayerWeight(1);
            _animator.SetLayerWeight(1, Mathf.MoveTowards(currentWeight, _targetLayer1Weight, Time.deltaTime * 10f));
        }
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
        if (info.isSuccessful) _networkAnimator.SetTrigger(_jumpHash);
    }

    private void HandleItemUse(ItemID itemID)
    {
        if (itemID == ItemID.BaseballBat) _networkAnimator.SetTrigger(_attackHash);
    }

    private void HandleInventoryChange(PlayerInventoryComponent.SlotChangeInfo info)
    {
        bool isHoldingItem = info.newSlotIndex != -1 && info.newItemData.itemID != ItemID.None;

        if (!isHoldingItem)
        {
            _animator.SetInteger(_holdTypeHash, 0);
            _aimIKTarget.DisableIK();
            _targetLayer1Weight = 0f;
            return;
        }

        switch (info.newItemData.itemID)
        {
            case ItemID.Map:
                _animator.SetInteger(_holdTypeHash, 2);
                _aimIKTarget.DisableIK();
                _targetLayer1Weight = 1f;
                break;
            case ItemID.BaseballBat:
            case ItemID.Taser:
            case ItemID.Torch:
            case ItemID.WalkieTalkie:
                _animator.SetInteger(_holdTypeHash, 1);
                _aimIKTarget.EnableIK();
                _targetLayer1Weight = 1f;
                break;
            default:
                _animator.SetInteger(_holdTypeHash, 0);
                _aimIKTarget.DisableIK();
                _targetLayer1Weight = 0f;
                break;
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