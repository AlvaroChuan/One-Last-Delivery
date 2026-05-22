using Mirror;
using UnityEngine;

public abstract class InventoryItem : NetworkBehaviour
{
    [SerializeField] private DroppedItem _droppedItem;
    public abstract void StartUse(GameObject user);
    public virtual void EndUse(GameObject user) { }
    public DroppedItem GetDroppedItemPrefab()
    {
        return _droppedItem;
    }
}