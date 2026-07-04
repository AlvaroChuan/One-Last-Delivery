using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PurchaseButton : MonoBehaviour
{
    enum PurchaseType
    {
        TruckUpgrade,
        Item
    }
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _price;
    [SerializeField] private Image _icon;
    private InventoryItemData _itemData;
    private TruckStatsStruct _truckUpgradeStats;
    private float _priceValue;
    private PurchaseType _purchaseType;

    void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnBuyButtonClicked);
        }
        else
        {
            Debug.LogError("PurchaseButton requires a Button component.");
        }
    }

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
            shopSystem.CmdRequestBuy(_truckUpgradeStats, _priceValue);
        }
        else if (_purchaseType == PurchaseType.Item)
        {
            shopSystem.CmdRequestBuy(_itemData, _priceValue);
        }
    }

    /// <summary>
    /// Sets the listing for the purchase button based on the provided TruckUpgrade or ItemListing. Updates the UI elements (name, price, icon) and stores the relevant data for purchase.
    /// </summary>
    /// <param name="truckUpgrade"></param>
    public void SetListing(TruckUpgrade truckUpgrade)
    {
        _truckUpgradeStats = truckUpgrade.Stats;
        _price.text = Mathf.RoundToInt(truckUpgrade.Price).ToString();
        _name.text = truckUpgrade.UpgradeName;
        _icon.sprite = truckUpgrade.UpgradeIcon?.sprite;
        _priceValue = truckUpgrade.Price;
        _purchaseType = PurchaseType.TruckUpgrade;
    }

    /// <summary>
    /// Sets the listing for the purchase button based on the provided ItemListing. Updates the UI elements (name, price, icon) and stores the relevant data for purchase.
    /// </summary>
    /// <param name="itemListing"></param>
    public void SetListing(ItemListing itemListing)
    {
        _itemData = itemListing.ItemData;
        _price.text = Mathf.RoundToInt(itemListing.Price).ToString();
        _name.text = itemListing.ItemName;
        _icon.sprite = itemListing.ItemIcon;
        _priceValue = itemListing.Price;
        _purchaseType = PurchaseType.Item;
    }
}