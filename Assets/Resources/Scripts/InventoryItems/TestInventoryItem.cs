using UnityEngine;

public class TestInventoryItem : InventoryItem
{
    public override void StartUse(GameObject user)
    {
        DevLogger.Log($"Started using {gameObject}!");
    }
    public override void EndUse(GameObject user)
    {
        DevLogger.Log($"Stopped using {gameObject}!");
    }
}
