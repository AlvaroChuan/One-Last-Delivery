using System;
using Mirror;
using UnityEngine;

public class ItemShopSystem : NetworkBehaviour
{
    public struct ItemPurchaseInfo
    {
        public bool purchaseSuccessful;
    }

    public Action<ItemPurchaseInfo> onPurchaseEvent;
    public static ItemShopSystem Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Handles the purchase request from a client. If the player has enough money, it deducts the amount and applies the upgrade or spawns the item. Otherwise, it notifies the client of the failed purchase.
    /// </summary>
    /// <param name="upgradeStats"></param>
    /// <param name="price"></param>
    /// <param name="conn"></param>
    [Command(requiresAuthority = false)]
    public void CmdRequestBuy(TruckStatsStruct upgradeStats, float price, NetworkConnectionToClient conn = null)
    {
        float money = MoneyManager.CurrentMoney;
        float balance = BalanceManager.GetBalance();
        float totalAvailableMoney = money + balance;
        if (totalAvailableMoney >= price)
        {
            BalanceManager.RegisterTransaction($"Purchased Truck Upgrade", -price); // Register the transaction
            TruckUpgradeManager.AddUpgradeStats(upgradeStats); // Apply the truck upgrade for the buyer
            TargetNotifyPurchaseSuccess(conn); // Notify the buyer of successful purchase
        }
        else
        {
            // Notify clients of failed purchase
            TargetNotifyPurchaseFailure(conn);
        }
    }

    /// <summary>
    /// Handles the purchase request from a client for an inventory item. If the player has enough money, it deducts the amount and spawns the item. Otherwise, it notifies the client of the failed purchase.
    /// </summary>
    /// <param name="itemData"></param>
    /// <param name="price"></param>
    /// <param name="conn"></param>
    [Command(requiresAuthority = false)]
    public void CmdRequestBuy(InventoryItemData itemData, float price, NetworkConnectionToClient conn = null)
    {
        float money = MoneyManager.CurrentMoney;
        float balance = BalanceManager.GetBalance();
        float totalAvailableMoney = money + balance;
        if (totalAvailableMoney >= price)
        {
            BalanceManager.RegisterTransaction($"Purchased Item", -price); // Register the transaction
            ServerSpawnItem(itemData, conn); // Spawn the item for the buyer
            TargetNotifyPurchaseSuccess(conn); // Notify the buyer of successful purchase
        }
        else
        {
            // Notify clients of failed purchase
            TargetNotifyPurchaseFailure(conn);
        }
    }

    [Server]
    public void ServerSpawnItem(InventoryItemData itemData, NetworkConnectionToClient conn = null)
    {
        DroppedItem itemPrefab = ItemDataList.Instance.GetPrefabFromID(itemData.itemID);
        GameObject buyer;
        if (conn != null && conn.identity != null)
        {
            buyer = conn.identity.gameObject;
        }
        else
        {
            buyer = NetworkClient.connection.identity.gameObject;
        }
        Vector3 forwardOffset = buyer.GetComponent<PlayerLookComponent>().Model.transform.forward; // Adjust the multiplier as needed for distance
        DroppedItem item = Instantiate(itemPrefab, buyer.transform.position + forwardOffset, Quaternion.identity);
        item.SetInventoryItemData(itemData);
        NetworkServer.Spawn(item.gameObject);
    }

    [TargetRpc]
    public void TargetNotifyPurchaseSuccess(NetworkConnectionToClient conn)
    {
        // Handle UI on clients for successful purchase
        onPurchaseEvent?.Invoke(new ItemPurchaseInfo
        {
            purchaseSuccessful = true
        });
    }

    [TargetRpc]
    public void TargetNotifyPurchaseFailure(NetworkConnectionToClient conn)
    {
        // Handle UI on clients for failed purchase
        onPurchaseEvent?.Invoke(new ItemPurchaseInfo
        {
            purchaseSuccessful = false
        });
    }
}