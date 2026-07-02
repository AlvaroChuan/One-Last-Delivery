using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerItemUseComponent : InputComponent
{
    public Action<ItemID> onItemUseStart; // Event to notify when an item is used
    public Action<ItemID> onItemUseStop; // Event to notify when an item use is stopped
    public Action<ItemID> onItemUseContinuous; // Event to notify when an item is being used continuously (for items that can be held down)
    [SerializeField] private InputActionReference _useInput;
    private PlayerInventoryComponent _inventoryComponent;
    InventoryItemData? _usingItemDataNullable;
    InventoryItem _usingItem;

    void Awake()
    {
        _inventoryComponent = GetComponent<PlayerInventoryComponent>();
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

        if (heldItemData.currentDurability <= 0 && !heldItemData.infiniteDurability) return; // Don't use the item if it's out of durability

        InventoryItem item = _inventoryComponent.GetHeldItem();
        item.StartUse(gameObject);

        if (heldItemData.oneShot)
        {
            if (!heldItemData.infiniteDurability)
            {
                heldItemData.currentDurability -= heldItemData.durabilityCost;

                if (heldItemData.currentDurability < 0)
                {
                    heldItemData.currentDurability = 0; // Ensure durability doesn't go below 0
                }

                _inventoryComponent.UpdateHeldItemData(heldItemData);
            }
        }
        else
        {
            _usingItemDataNullable = heldItemData; // Store the item data for durability management in Update
            _usingItem = item; // Store the reference to the using item to call EndUse later
        }
        onItemUseStart?.Invoke(heldItemData.itemID);
    }

    private void OnUseInputCanceled(InputAction.CallbackContext context)
    {
        StopUsingItem();
    }

    void StopUsingItem()
    {
        if (_usingItem == null) return;

        _usingItem.EndUse(gameObject);
        onItemUseStop?.Invoke(_usingItemDataNullable.Value.itemID);
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
            usingItemData.currentDurability -= usingItemData.durabilityCost * Time.deltaTime;
            if (usingItemData.currentDurability <= 0)
            {
                usingItemData.currentDurability = 0; // Ensure durability doesn't go below 0

                StopUsingItem();
            }
            _inventoryComponent.UpdateHeldItemData(usingItemData);
            _usingItemDataNullable = usingItemData; // Update the stored item data with the new durability value
        }

        onItemUseContinuous?.Invoke(usingItemData.itemID);
    }
}