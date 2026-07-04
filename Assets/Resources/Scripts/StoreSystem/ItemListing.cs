using UnityEngine;

[CreateAssetMenu(fileName = "NewItemListing", menuName = "Store/Item Listing")]
public class ItemListing : ScriptableObject
{
    [SerializeField] private InventoryItemData _itemData;
    [SerializeField] private float _price;
    [SerializeField] private string _itemName;
    [SerializeField] private Sprite _itemIcon;

    public InventoryItemData ItemData => _itemData;
    public float Price => _price;
    public string ItemName => _itemName;
    public Sprite ItemIcon => _itemIcon;
}