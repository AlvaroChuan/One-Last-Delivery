using UnityEngine;

public class TestInventoryItem : InventoryItem
{
    public override void StartUse(GameObject user)
    {
        Debug.Log($"Started using {gameObject}!");
    }
    public override void EndUse(GameObject user)
    {
        Debug.Log($"Stopped using {gameObject}!");
    }
}
