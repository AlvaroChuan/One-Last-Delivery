using Mirror;
using UnityEngine;

[RequireComponent(typeof(HideOnDeath))]
public class InventoryHUD : MonoBehaviour
{
    [System.Serializable]
    struct ItemIconMapping
    {
        public ItemID itemID;
        public Sprite icon;
    }
    [SerializeField] private ItemIconMapping[] _itemIconMappings;
    [SerializeField] private InventorySlotUI[] _inventorySlots;
    private InventoryItemData[] _inventoryItems;
    private PlayerInventoryManager _playerInventoryManager;
    private PlayerInventoryComponent _playerInventoryComponent;

    void Update()
    {
        if (_playerInventoryManager == null)
        {
            _playerInventoryManager = FindAnyObjectByType<PlayerInventoryManager>();
            if (_playerInventoryManager != null)
            {
                DevLogger.Log("PlayerInventoryManager found and subscribed to onDataChangedEvent.");
                _playerInventoryManager.onDataChangedEvent += OnInventoryDataChanged;
                for(int i = 0; i < _inventorySlots.Length; i++)
                {
                    InventoryItemData itemData = _playerInventoryManager.GetInventorySlot(i);
                    _inventorySlots[i].UpdateSlot(itemData, GetIconForItem(itemData.itemID));
                }
            }
        }
        if (_playerInventoryComponent == null && NetworkClient.connection != null && NetworkClient.connection.identity != null)
        {
            _playerInventoryComponent = NetworkClient.connection.identity.GetComponent<PlayerInventoryComponent>();
            if (_playerInventoryComponent != null)
            {
                DevLogger.Log("PlayerInventoryComponent found and subscribed to onInventorySlotChanged.");
                _playerInventoryComponent.onInventorySlotChangedOwner += OnSelectionChanged;
            }
        }
    }

    void OnDestroy()
    {
        if (_playerInventoryManager != null)
        {
            _playerInventoryManager.onDataChangedEvent -= OnInventoryDataChanged;
        }

        if (_playerInventoryComponent != null)
        {
            _playerInventoryComponent.onInventorySlotChangedOwner -= OnSelectionChanged;
        }
    }

    private void OnSelectionChanged(PlayerInventoryComponent.SlotChangeInfo slotChangeInfo)
    {
        int index = slotChangeInfo.newSlotIndex;
        for(int i = 0; i < _inventorySlots.Length; i++)
        {
            _inventorySlots[i].SetSelected(i == index);
        }
    }

    private void OnInventoryDataChanged(PersistentDataManager<PlayerInventoryManager, PlayerInventoryManager.PlayerInventoryStaticState, PlayerInventoryManager.Inventory>.DataChangeInfo info)
    {
        PlayerInventoryManager.Inventory inventory = info.newValue;
        if (inventory != null && inventory.items != null)
        {
            _inventoryItems = inventory.items.ToArray();
            UpdateInventoryUI();
        }
        else
        {
            DevLogger.LogWarning("Inventory data is null or items list is null.");
        }
    }

    void UpdateInventoryUI()
    {
        for (int i = 0; i < _inventorySlots.Length; i++)
        {
            InventoryItemData itemData = (i < _inventoryItems.Length) ? _inventoryItems[i] : new InventoryItemData { itemID = ItemID.None };
            _inventorySlots[i].UpdateSlot(itemData, GetIconForItem(itemData.itemID));
        }
    }

    Sprite GetIconForItem(ItemID itemID)
    {
        foreach (var mapping in _itemIconMappings)
        {
            if (mapping.itemID == itemID)
            {
                return mapping.icon;
            }
        }
        return null; // Return null if no icon is found for the given ItemID
    }
}
