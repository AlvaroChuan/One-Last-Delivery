using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryManager : PersistentDataManager<PlayerInventoryManager, PlayerInventoryManager.PlayerInventoryStaticState, PlayerInventoryManager.Inventory>
{
    public class Inventory
    {
        public List<InventoryItemData> items = new List<InventoryItemData>();
    }
    public class PlayerInventoryStaticState : StaticStateBase
    {
        public override void Reset()
        {
            StaticData.items.Clear();
        }
    }
    public void SetInventorySlot(int index, InventoryItemData itemData)
    {
        if(index < 0)
        {
            Debug.LogWarning($"Attempted to set inventory slot {index} which is out of bounds.");
            return;
        }
        if(StaticDataState.StaticData.items == null || index >= StaticDataState.StaticData.items.Count)
        {
            // Resize the list to accommodate the new index
            while(StaticDataState.StaticData.items.Count <= index)
            {
                StaticDataState.StaticData.items.Add(new InventoryItemData { itemID = ItemID.None });
            }
        }
        StaticDataState.StaticData.items[index] = itemData;
    }
    public InventoryItemData GetInventorySlot(int index)
    {
        if(index < 0 || StaticDataState.StaticData.items == null || index >= StaticDataState.StaticData.items.Count)
        {
            Debug.LogWarning($"Attempted to get inventory slot {index} which is out of bounds.");
            return new InventoryItemData { itemID = ItemID.None };
        }
        return StaticDataState.StaticData.items[index];
    }
}