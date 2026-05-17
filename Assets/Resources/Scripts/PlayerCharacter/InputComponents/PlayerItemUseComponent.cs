using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerItemUseComponent : InputComponent
{
    [SerializeField] private InputActionReference _useInput;
    private InventoryComponent _inventoryComponent;
    InventoryItemData? _usingItemDataNullable;
    InventoryItem _usingItem;

    void Awake()
    {
        _inventoryComponent = GetComponent<InventoryComponent>();
    }

    protected override void BindInputs()
    {
        if (!isLocalPlayer) return;

        _useInput.action.Enable();
        _useInput.action.performed += OnUseInputTriggered;
        _useInput.action.canceled += OnUseInputCanceled;
    }

    protected override void UnbindInputs()
    {
        if (!isLocalPlayer) return;

        _useInput.action.Disable();
        _useInput.action.performed -= OnUseInputTriggered;
        _useInput.action.canceled -= OnUseInputCanceled;
    }

    private void OnUseInputTriggered(InputAction.CallbackContext context)
    {
        InventoryItemData heldItemData = _inventoryComponent.GetHeldItemData();

        if (heldItemData.IsEmpty) return;

        if (heldItemData.durability <= 0 && !heldItemData.infiniteDurability) return; // Don't use the item if it's out of durability

        if (heldItemData.oneShot)
        {
            InventoryItem item = _inventoryComponent.GetHeldItem();
            item.StartUse(gameObject);
            if (!heldItemData.infiniteDurability)
            {
                heldItemData.durability -= heldItemData.durabilityCost;
                _inventoryComponent.UpdateHeldItemData(heldItemData);
            }
        }
        else
        {
            InventoryItem item = _inventoryComponent.GetHeldItem();
            item.StartUse(gameObject);

            _usingItemDataNullable = heldItemData; // Store the item data for durability management in Update
            _usingItem = item; // Store the reference to the using item to call EndUse later
        }
    }

    private void OnUseInputCanceled(InputAction.CallbackContext context)
    {
        StopUsingItem();
    }

    void StopUsingItem()
    {
        if (_usingItem == null) return;

        _usingItem.EndUse(gameObject);
        _usingItem = null;
        _usingItemDataNullable = null;
    }

    void Update()
    {
        if (_usingItem == null) return;

        InventoryItemData usingItemData = _usingItemDataNullable.Value;

        //Check if the item being used is still the currently held item, if not stop using it
        if (!usingItemData.Equals(_inventoryComponent.GetHeldItemData()))
        {
            StopUsingItem();
            return;
        }

        if (usingItemData.IsEmpty) return;

        if (!usingItemData.infiniteDurability)
        {
            usingItemData.durability -= usingItemData.durabilityCost * Time.deltaTime;
            _inventoryComponent.UpdateHeldItemData(usingItemData);
            _usingItemDataNullable = usingItemData; // Update the stored item data with the new durability value

            if (usingItemData.durability <= 0)
            {
                StopUsingItem();
            }
        }
    }
}