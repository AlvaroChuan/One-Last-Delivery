using System;
using Mirror;
using UnityEngine;

public abstract class InventoryItem : NetworkBehaviour
{
    protected Action _durabilityCallback;
    public abstract void StartUse(GameObject user);
    public virtual void EndUse(GameObject user) { }

    public virtual void SetDurabilityCallback(Action durabilityCallback)
    {
        if (durabilityCallback == null || _durabilityCallback == null)
        {
            _durabilityCallback = durabilityCallback;
        }
    }
}