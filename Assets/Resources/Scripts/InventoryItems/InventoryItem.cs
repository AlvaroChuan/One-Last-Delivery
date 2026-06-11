using Mirror;
using UnityEngine;

public abstract class InventoryItem : NetworkBehaviour
{
    public abstract void StartUse(GameObject user);
    public virtual void EndUse(GameObject user) { }
}