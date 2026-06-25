using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NewItemListing", menuName = "Store/Item Listing")]
public class ItemListing : ScriptableObject
{
    [SerializeField] private InventoryItemData _itemData;
    [SerializeField] private float _price;
    [SerializeField] private string _itemName;
    [SerializeField] private Image _itemIcon;

    public InventoryItemData ItemData => _itemData;
    public float Price => _price;
    public string ItemName => _itemName;
    public Image ItemIcon => _itemIcon;
}