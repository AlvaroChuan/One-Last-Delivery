using UnityEngine;

[CreateAssetMenu(fileName = "ItemDataList", menuName = "ScriptableObjects/ItemDataList", order = 1)]
public class ItemDataList : ScriptableObject
{
    private static ItemDataList _instance;
    public static ItemDataList Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ItemDataList>("ScriptableObjects/ItemDataList");
                if (_instance == null)
                {
                    Debug.LogError("ItemDataList asset not found in Resources folder!");
                }
            }
            return _instance;
        }
    }
    [System.Serializable]
    class ItemEntry
    {
        public ItemID itemID;
        public DroppedItem itemPrefab;
    }
    [SerializeField] private ItemEntry[] _itemEntries;
    public DroppedItem GetPrefabFromID(ItemID itemID)
    {
        foreach (var entry in _itemEntries)
        {
            if (entry.itemID == itemID)
            {
                return entry.itemPrefab;
            }
        }
        Debug.LogWarning($"Dropped item with ID {itemID} not found in ItemDataList.");
        return null;
    }
    public DroppedItem GetPrefabFromID(int itemID)
    {
        return GetPrefabFromID((ItemID)itemID);
    }
}