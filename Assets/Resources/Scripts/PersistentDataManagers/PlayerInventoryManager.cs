using UnityEngine;

public class PlayerInventoryManager : PersistentDataManager<PlayerInventoryManager, PlayerInventoryManager.StaticState, PlayerInventoryManager.Inventory>
{
    public class Inventory
    {
        public InventoryItemData[] items = new InventoryItemData[0];
    }
    public class StaticState : StaticStateBase
    {
        public override void Reset()
        {
            StaticData = new Inventory { items = new InventoryItemData[0] };
        }
    }
    public void SetInventorySlot(int index, InventoryItemData itemData)
    {
        if(index < 0)
        {
            Debug.LogWarning($"Attempted to set inventory slot {index} which is out of bounds.");
            return;
        }
        if(StaticDataState.StaticData.items == null || index >= StaticDataState.StaticData.items.Length)
        {
            // Resize the array to accommodate the new index
            InventoryItemData[] newItems = new InventoryItemData[index + 1];
            if(StaticDataState.StaticData.items != null)
            {
                StaticDataState.StaticData.items.CopyTo(newItems, 0);
            }
            StaticDataState.StaticData.items = newItems;
        }
        StaticDataState.StaticData.items[index] = itemData;
    }
    public InventoryItemData GetInventorySlot(int index)
    {
        if(index < 0 || StaticDataState.StaticData.items == null || index >= StaticDataState.StaticData.items.Length)
        {
            Debug.LogWarning($"Attempted to get inventory slot {index} which is out of bounds.");
            return new InventoryItemData { itemID = ItemID.None };
        }
        return StaticDataState.StaticData.items[index];
    }
}