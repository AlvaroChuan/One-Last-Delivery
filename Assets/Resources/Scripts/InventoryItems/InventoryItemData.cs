using UnityEngine;

[System.Serializable]
public struct InventoryItemData
{
    public ItemID itemID;
    [Tooltip("If true, the item is used instantly on use input and doesn't require holding down the use button.")]
    public bool oneShot;
    public bool durabilityOnCallbackOnly; // If true, durability is only reduced when the item use callback is invoked, not on every use.
    public bool infiniteDurability;
    public float maxDurability;
    public float currentDurability;
    [Tooltip("For one-shot items, how much durability to consume on use. For hold items, how much durability to consume per second while using.")]
    public float durabilityCost;

    // A helper property to check if the slot is actually empty
    public bool IsEmpty => itemID == ItemID.None;
}