using Mirror;
using UnityEngine;

public class StoreMenu : MonoBehaviour
{
    [Header("Store Listings")]
    [SerializeField] private ItemListing[] _itemListings;
    [SerializeField] private TruckUpgrade[] _truckUpgrades;
    [Header("UI Elements")]
    [SerializeField] private Transform _itemButtonContainer;
    [SerializeField] private Transform _truckUpgradeButtonContainer;
    [Header("Prefabs")]
    [SerializeField] private PurchaseButton _purchaseButtonPrefab;

    private void Start()
    {
        PopulateStore();
    }

    void PopulateStore()
    {
        // Populate item listings
        foreach (var itemListing in _itemListings)
        {
            var button = Instantiate(_purchaseButtonPrefab, _itemButtonContainer);
            button.SetListing(itemListing);
        }

        // Populate truck upgrades
        foreach (var truckUpgrade in _truckUpgrades)
        {
            var button = Instantiate(_purchaseButtonPrefab, _truckUpgradeButtonContainer);
            button.SetListing(truckUpgrade);
        }
    }
}