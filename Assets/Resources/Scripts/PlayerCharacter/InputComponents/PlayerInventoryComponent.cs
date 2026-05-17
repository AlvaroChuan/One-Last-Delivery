using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventoryComponent : InputComponent
{
    [System.Serializable]
    struct ItemEntry
    {
        public InventoryItemIDEnum itemID;
        public InventoryItem item;
    }
    [SerializeField] private int _inventorySize = 4;
    [SerializeField] private InputActionReference _scrollInput;
    [SerializeField] private InputActionReference _selectInput;
    [SerializeField] private InputActionReference _dropInput;
    [SerializeField] private InputActionReference _throwInput;
    [SerializeField] private float _throwForce = 10f;
    [SerializeField] private ItemEntry[] _itemEntryArray;
    private InventoryItemData[] _inventory;
    private int _selectedInventoryIndex = -1; // Local copy of the selected index for instant responsiveness

    private Camera _playerCamera;

    private PackageInteractionComponent _carriedPackage; // Reference to the currently carried package, if any

    void Awake()
    {
        _playerCamera = GetComponentInChildren<Camera>();
        _inventory = new InventoryItemData[_inventorySize];
        for(int i = 0; i < _inventorySize; i++)
        {
            _inventory[i] = new InventoryItemData { itemID = (int)InventoryItemIDEnum.None };
        }
    }

    protected override void BindInputs()
    {
        if (!isLocalPlayer) return;

        _scrollInput.action.Enable();
        _scrollInput.action.performed += OnScrollInput;

        _selectInput.action.Enable();
        _selectInput.action.performed += OnSelectInput;

        _dropInput.action.Enable();
        _dropInput.action.performed += OnDropInput;

        _throwInput.action.Enable();
        _throwInput.action.performed += OnThrowInput;
    }

    protected override void UnbindInputs()
    {
        if (!isLocalPlayer) return;

        _scrollInput.action.Disable();
        _scrollInput.action.performed -= OnScrollInput;

        _selectInput.action.Disable();
        _selectInput.action.performed -= OnSelectInput;

        _dropInput.action.Disable();
        _dropInput.action.performed -= OnDropInput;

        _throwInput.action.Disable();
        _throwInput.action.performed -= OnThrowInput;
    }

    private void OnScrollInput(InputAction.CallbackContext context)
    {
        float scrollValue = context.ReadValue<float>();
        if (scrollValue > 0f)
        {
            _selectedInventoryIndex++;
        }
        else if (scrollValue < 0f)
        {
            _selectedInventoryIndex--;
        }

        if (_selectedInventoryIndex >= _inventorySize)
        {
            _selectedInventoryIndex = -1;
        }
        else if (_selectedInventoryIndex < -1)
        {
            _selectedInventoryIndex = _inventorySize - 1;
        }

        if (_carriedPackage != null)
        {
            _carriedPackage.CmdDropFromPlayer(Vector3.zero);
            _carriedPackage = null;
        }

        Debug.Log("Selected inventory index: " + _selectedInventoryIndex);
        SetInventorySelection(_selectedInventoryIndex);
    }
    private void OnSelectInput(InputAction.CallbackContext context)
    {
        float selectValue = context.ReadValue<float>();
        int index = Mathf.FloorToInt(selectValue) - 1;
        if (index == _selectedInventoryIndex)
        {
            _selectedInventoryIndex = -1;
        }
        else if (index >= 0 && index < _inventorySize)
        {
            _selectedInventoryIndex = index;
        }
        if (_carriedPackage != null)
        {
            _carriedPackage.CmdDropFromPlayer(Vector3.zero);
            _carriedPackage = null;
        }
        SetInventorySelection(_selectedInventoryIndex);
    }

    void SetInventorySelection(int index)
    {
        _selectedInventoryIndex = index;
        InventoryItemIDEnum itemID = InventoryItemIDEnum.None;
        if (index >= 0 && index < _inventorySize)
        {
            itemID = _inventory[index].itemID;
        }
        UpdateVisualMesh(itemID);
        CmdUpdateVisualMesh(itemID);
    }

    [Command]
    void CmdUpdateVisualMesh(InventoryItemIDEnum itemID)
    {
        RpcUpdateVisualMesh(itemID);
    }

    [ClientRpc]
    void RpcUpdateVisualMesh(InventoryItemIDEnum newItemID)
    {
        if (isLocalPlayer) return; // Local player already updated their visuals in SetInventorySelection, so only update for remote clients

        UpdateVisualMesh(newItemID);
    }

    void UpdateVisualMesh(InventoryItemIDEnum itemID)
    {
        foreach (var entry in _itemEntryArray)
        {
            if (entry.itemID == itemID)
            {
                entry.item.gameObject.SetActive(true);
            }
            else
            {
                entry.item.gameObject.SetActive(false);
            }
        }
    }

    private void OnDropInput(InputAction.CallbackContext context)
    {
        DropItem(_selectedInventoryIndex);
    }

    private void OnThrowInput(InputAction.CallbackContext context)
    {
        DropItem(_selectedInventoryIndex, _playerCamera.transform.forward * _throwForce);
    }

    void DropItem(int slotIndex, Vector3 throwForce = default)
    {
        InventoryItem heldItem = GetItem(slotIndex);
        if (heldItem != null)
        {
            DroppedItem droppedItem = heldItem.GetDroppedItemPrefab();
            if (droppedItem != null)
            {
                // Spawn the dropped item on the server
                CmdSpawnDroppedItem(_inventory[slotIndex], transform.position + transform.forward, throwForce);

                // Clear the inventory slot locally for instant feedback
                _inventory[slotIndex] = new InventoryItemData { itemID = InventoryItemIDEnum.None };
                if (_selectedInventoryIndex == slotIndex)
                {
                    SetInventorySelection(slotIndex); // This will also update visuals and sync the change to other clients
                }
            }
        }

        if (_carriedPackage != null)
        {
            _carriedPackage.CmdDropFromPlayer(throwForce);
            _carriedPackage = null;
        }
    }

    // Mirror allows passing NetworkIdentity references of Registered Spawnable Prefabs inside Commands!
    [Command]
    private void CmdSpawnDroppedItem(InventoryItemData itemData, Vector3 dropPosition, Vector3 throwForce)
    {
        GameObject droppedItemPrefab = null;
        foreach (var entry in _itemEntryArray)
        {
            if (entry.itemID == itemData.itemID)
            {
                droppedItemPrefab = entry.item.GetDroppedItemPrefab().gameObject;
                break;
            }
        }

        // Instantiate and spawn the object authoritatively on the server
        GameObject droppedObject = Instantiate(droppedItemPrefab, dropPosition, Quaternion.identity);
        droppedObject.GetComponent<DroppedItem>().SetInventoryItemData(itemData);
        droppedObject.GetComponent<Rigidbody>().AddForce(throwForce, ForceMode.VelocityChange);
        NetworkServer.Spawn(droppedObject);
    }

    [ClientRpc]
    public void RpcAddItem(InventoryItemData itemData)
    {
        if (!isLocalPlayer) return;

        Debug.Log($"Adding item {itemData.itemID} to inventory");

        int affectedSlot = -1;
        if (affectedSlot == -1)
        {
            for (int i = 0; i < _inventorySize; i++)
            {
                if (_inventory[i].itemID == (int)InventoryItemIDEnum.None)
                {
                    affectedSlot = i;
                    break;
                }
            }
        }
        if (affectedSlot == -1 && _selectedInventoryIndex >= 0 && _selectedInventoryIndex < _inventorySize)
        {
            affectedSlot = _selectedInventoryIndex;
        }
        if (affectedSlot == -1)
        {
            affectedSlot = 0; // If inventory is full, overwrite the first slot (could be changed to a different behavior like dropping the currently held item or refusing the new item)
        }

        DropItem(affectedSlot); // Drop currently held item if there is one, to free up the slot for the new item

        _inventory[affectedSlot] = itemData;

        SetInventorySelection(affectedSlot); // This will also update visuals and sync the change to other clients
    }

    private InventoryItem GetItem(int slotIndex)
    {
        if(slotIndex >= 0 && slotIndex < _inventorySize)
        {
            InventoryItemData itemData = _inventory[slotIndex];
            foreach (var entry in _itemEntryArray)
            {
                if (entry.itemID == itemData.itemID)
                {
                    return entry.item;
                }
            }
        }
        return null;
    }

    public InventoryItem GetHeldItem()
    {
        return GetItem(_selectedInventoryIndex);
    }
    public InventoryItemData GetHeldItemData()
    {
        if (_selectedInventoryIndex >= 0 && _selectedInventoryIndex < _inventorySize)
        {
            return _inventory[_selectedInventoryIndex];
        }
        return new InventoryItemData { itemID = InventoryItemIDEnum.None };
    }
    public void UpdateHeldItemData(InventoryItemData newData)
    {
        if (_selectedInventoryIndex >= 0 && _selectedInventoryIndex < _inventorySize)
        {
            _inventory[_selectedInventoryIndex] = newData;
        }
    }

    public void SetSlotSelection(int index)
    {
        if (!isLocalPlayer) return;

        SetInventorySelection(index);
    }

    internal void SetCarryingPackage(PackageInteractionComponent package)
    {
        _carriedPackage = package;
    }
}