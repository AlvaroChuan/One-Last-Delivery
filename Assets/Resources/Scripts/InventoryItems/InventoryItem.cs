using System;
using Mirror;
using UnityEngine;

public abstract class InventoryItem : NetworkBehaviour
{
    protected Action _durabilityCallback;
    public abstract void StartUse(GameObject user);
    public virtual void EndUse(GameObject user) { }

    public virtual void SetCallback(Action durabilityCallback)
    {
        if (durabilityCallback == null || _durabilityCallback == null)
        {
            _durabilityCallback = durabilityCallback;
        }
    }
}