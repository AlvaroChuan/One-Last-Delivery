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
    bool _storeAvailable = true;

    private void Start()
    {
        PopulateStore();
        SunManager.OnNightfall += OnNightfall;
    }

    void OnDestroy()
    {
        SunManager.OnNightfall -= OnNightfall;
    }

    void OnEnable()
    {
        if (!_storeAvailable)
        {
            gameObject.SetActive(false);
        }
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

    public void OnNightfall()
    {
        _storeAvailable = false;
        // Optionally, you can disable the store UI or show a message indicating that the store is closed.
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }
}