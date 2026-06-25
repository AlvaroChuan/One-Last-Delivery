using System;
using Mirror;
using UnityEngine;

public class ItemShopSystem : NetworkBehaviour
{
    public struct ItemPurchaseInfo
    {
        public NetworkConnectionToClient buyerConnection;
        public bool purchaseSuccessful;
    }

    public Action<ItemPurchaseInfo> onItemPurchasedEvent;
    public static ItemShopSystem Instance { get; private set; }

    [SerializeField] Transform _itemSpawnPoint;

    void Awake()
    {
        Instance = this;
    }

    [Command]
    public void CmdRequestBuyTruckUpgrade(TruckStatsStruct upgradeStats, float price, NetworkConnectionToClient conn = null)
    {
        if (MoneyManager.ServerSubtractMoney(price))
        {
            TruckUpgradeManager.AddUpgradeStats(upgradeStats); // Apply the truck upgrade for the buyer
            TargetNotifyPurchaseSuccess(conn); // Notify the buyer of successful purchase
        }
        else
        {
            // Notify clients of failed purchase
            TargetNotifyPurchaseFailure(conn);
        }
    }

    [Command]
    public void CmdRequestBuyItem(InventoryItemData itemData, float price, NetworkConnectionToClient conn = null)
    {
        if (MoneyManager.ServerSubtractMoney(price))
        {
            ServerSpawnItem(itemData); // Spawn the item for the buyer
            TargetNotifyPurchaseSuccess(conn); // Notify the buyer of successful purchase
        }
        else
        {
            // Notify clients of failed purchase
            TargetNotifyPurchaseFailure(conn);
        }
    }

    [Server]
    public void ServerSpawnItem(InventoryItemData itemData)
    {
        DroppedItem itemPrefab = ItemDataList.Instance.GetPrefabFromID(itemData.itemID);
        DroppedItem item = Instantiate(itemPrefab, _itemSpawnPoint.position, _itemSpawnPoint.rotation);
        item.SetInventoryItemData(itemData);
        NetworkServer.Spawn(item.gameObject);
    }

    [TargetRpc]
    public void TargetNotifyPurchaseSuccess(NetworkConnectionToClient conn)
    {
        // Handle UI on clients for successful purchase
        onItemPurchasedEvent?.Invoke(new ItemPurchaseInfo
        {
            buyerConnection = conn,
            purchaseSuccessful = true
        });
    }

    [TargetRpc]
    public void TargetNotifyPurchaseFailure(NetworkConnectionToClient conn)
    {
        // Handle UI on clients for failed purchase
        onItemPurchasedEvent?.Invoke(new ItemPurchaseInfo
        {
            buyerConnection = conn,
            purchaseSuccessful = false
        });
    }
}