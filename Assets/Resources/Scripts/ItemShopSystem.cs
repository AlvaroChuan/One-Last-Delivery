using Mirror;
using UnityEngine;

public class ItemShopSystem : NetworkBehaviour
{
    public static ItemShopSystem Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    [Command]
    public void CmdRequestBuyItem(InventoryItemData itemData, int price, NetworkConnectionToClient conn = null)
    {
        if (MoneyManager.Instance.ServerSubtractMoney(price))
        {
            ServerSpawnItem(conn.identity.gameObject, itemData); // Spawn the item for the buyer
            TargetBuyItemSuccess(conn); // Notify the buyer of successful purchase
        }
        else
        {
            // Notify clients of failed purchase
            TargetBuyItemFailure(conn);
        }
    }

    [Server]
    public void ServerSpawnItem(GameObject buyer, InventoryItemData itemData)
    {
        DroppedItem itemPrefab = ItemDataList.Instance.GetPrefabFromID(itemData.itemID);
        DroppedItem item = Instantiate(itemPrefab, buyer.transform.position + buyer.transform.forward, buyer.transform.rotation);
        item.SetInventoryItemData(itemData);
        NetworkServer.Spawn(item.gameObject);
    }

    [TargetRpc]
    public void TargetBuyItemSuccess(NetworkConnectionToClient conn)
    {
        // Handle UI on clients for successful purchase
    }

    [TargetRpc]
    public void TargetBuyItemFailure(NetworkConnectionToClient conn)
    {
        // Handle UI on clients for failed purchase
    }
}