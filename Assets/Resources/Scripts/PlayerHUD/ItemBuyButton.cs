using UnityEngine;

public class ItemBuyButton : MonoBehaviour
{
    [SerializeField] private InventoryItemData _itemData;
    [SerializeField] private int _price;
    public void OnBuyButtonClicked()
    {
        ItemShopSystem shopSystem = ItemShopSystem.Instance;
        if (shopSystem != null)
        {
            shopSystem.CmdRequestBuyItem(_itemData, _price); // Call the command to request buying the item
        }
    }
}