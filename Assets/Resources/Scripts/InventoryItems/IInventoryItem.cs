using UnityEngine;
using Mirror;

public abstract class InventoryItem : NetworkBehaviour
{
    public abstract void Use(GameObject user);
    public abstract Interactable GetInteractablePrefab();
}