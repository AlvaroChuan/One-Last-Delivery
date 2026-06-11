using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(Rigidbody))]
public class DroppedItem : Interactable
{
    void Start()
    {
        Debug.Log($"DroppedItem {gameObject.name} network identity: {GetComponent<NetworkIdentity>().netId}, isServer: {isServer}, isClient: {isClient}");
    }
    [SerializeField] private InventoryItemData _inventoryItemData;
    bool _pickedUp = false;
    public override void Interact(GameObject interactor)
    {
        // This should only run on the server since the item pickup logic is server-authoritative
        if (!isServer || _pickedUp) return;

        Debug.Log($"Player {interactor.name} interacted with dropped item {gameObject.name} containing {GetInventoryItemData().itemID}");

        PlayerInventoryComponent inventory = interactor.GetComponent<PlayerInventoryComponent>();
        if (inventory != null)
        {
            _pickedUp = true;
            // Add the item to the player's inventory
            inventory.RpcAddItem(_inventoryItemData);

            // Destroy the world object after pickup
            NetworkServer.Destroy(gameObject);
            Debug.Log($"Item {gameObject.name} picked up by {interactor.name} and destroyed in the world.");
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