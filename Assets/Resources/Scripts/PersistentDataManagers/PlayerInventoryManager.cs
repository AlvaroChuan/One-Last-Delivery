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
    protected override void Awake()
    {
        base.Awake();
        if (StaticDataState.StaticData == null)
        {
            StaticDataState.StaticData = new Inventory();
        }
    }
    public void SetInventorySlot(int index, InventoryItemData itemData)
    {
        if(index < 0)
        {
            Debug.LogWarning($"Attempted to set inventory slot {index} which is out of bounds.");
            return;
        }
        Inventory inventory = StaticDataState.StaticData;
        while (inventory.items.Count <= index)
        {
            inventory.items.Add(new InventoryItemData { itemID = ItemID.None });
        }
        inventory.items[index] = itemData;
        StaticDataState.StaticData = inventory;
    }
    public InventoryItemData GetInventorySlot(int index)
    {
        if(index < 0)
        {
            Debug.LogWarning($"Attempted to get inventory slot {index} which is out of bounds.");
            return new InventoryItemData { itemID = ItemID.None };
        }
        if(StaticDataState.StaticData.items == null || index >= StaticDataState.StaticData.items.Count)
        {
            return new InventoryItemData { itemID = ItemID.None };
        }
        return StaticDataState.StaticData.items[index];
    }
}