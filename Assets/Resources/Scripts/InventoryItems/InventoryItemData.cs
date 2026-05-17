using UnityEngine;

[System.Serializable]
public struct InventoryItemData
{
    public InventoryItemIDEnum itemID;
    [Tooltip("If true, the item is used instantly on use input and doesn't require holding down the use button.")]
    public bool oneShot;
    public bool infiniteDurability;
    public float durability;
    [Tooltip("For one-shot items, how much durability to consume on use. For hold items, how much durability to consume per second while using.")]
    public float durabilityCost;

    // A helper property to check if the slot is actually empty
    public bool IsEmpty => itemID == InventoryItemIDEnum.None;
}