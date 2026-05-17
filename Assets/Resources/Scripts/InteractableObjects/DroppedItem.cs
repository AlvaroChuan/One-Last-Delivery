using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(Rigidbody))]
public class DroppedItem : Interactable
{
    [SerializeField] private InventoryItemData _inventoryItemData;
    public override void Interact(GameObject interactor)
    {
        // This should only run on the server since the item pickup logic is server-authoritative
        if (!isServer) return;

        Debug.Log($"Player {interactor.name} interacted with dropped item {gameObject.name} containing {GetInventoryItemData().itemID}");

        InventoryComponent inventory = interactor.GetComponent<InventoryComponent>();
        if (inventory != null)
        {
            // Add the item to the player's inventory
            inventory.RpcAddItem(_inventoryItemData);

            // Destroy the world object after pickup
            NetworkServer.Destroy(gameObject);
        }
    }
    public void SetInventoryItemData(InventoryItemData itemData)
    {
        _inventoryItemData = itemData;
    }
    public InventoryItemData GetInventoryItemData()
    {
        return _inventoryItemData;
    }
}