using UnityEngine;
using UnityEngine.UI;

public class PurchaseButton : MonoBehaviour
{
    enum PurchaseType
    {
        TruckUpgrade,
        Item
    }
    [SerializeField] private Text _name;
    [SerializeField] private Text _price;
    [SerializeField] private Image _icon;
    private InventoryItemData _itemData;
    private TruckStatsStruct _truckUpgradeStats;
    private float _priceValue;
    private PurchaseType _purchaseType;

    public void OnBuyButtonClicked()
    {
        ItemShopSystem shopSystem = ItemShopSystem.Instance;
        if (shopSystem == null)
        {
            DevLogger.LogError("ItemShopSystem instance not found.");
            return;
        }

        if (_purchaseType == PurchaseType.TruckUpgrade)
        {
            shopSystem.CmdRequestBuyTruckUpgrade(_truckUpgradeStats, _priceValue);
        }
        else if (_purchaseType == PurchaseType.Item)
        {
            shopSystem.CmdRequestBuyItem(_itemData, _priceValue);
        }
    }

    public void SetTruckUpgrade(TruckUpgrade truckUpgrade)
    {
        _truckUpgradeStats = truckUpgrade.Stats;
        _price.text = truckUpgrade.Price.ToString("F2");
        _name.text = truckUpgrade.UpgradeName;
        _icon.sprite = truckUpgrade.UpgradeIcon.sprite;
        _priceValue = truckUpgrade.Price;
        _purchaseType = PurchaseType.TruckUpgrade;
    }
    public void SetItemListing(ItemListing itemListing)
    {
        _itemData = itemListing.ItemData;
        _price.text = itemListing.Price.ToString("F2");
        _name.text = itemListing.ItemName;
        _icon.sprite = itemListing.ItemIcon.sprite;
        _priceValue = itemListing.Price;
        _purchaseType = PurchaseType.Item;
    }
}