using System;
using Mirror;
using UnityEngine;

public class Torch : InventoryItem
{
    [SerializeField] private Light _torchLight;
    [SyncVar(hook = nameof(OnTorchSwitched))] private bool _isTorchOn = false;
    override public void StartUse(GameObject user)
    {
        // Implement torch usage logic here
        Debug.Log($"{user.name} has started using the torch.");
        _torchLight.enabled = true;
        CmdSwitchTorch(true);
    }
    override public void EndUse(GameObject user)
    {
        // Implement logic for when the torch is no longer being used
        Debug.Log($"{user.name} has stopped using the torch.");
        _torchLight.enabled = false;
        CmdSwitchTorch(false);
    }
    [Command]
    void CmdSwitchTorch(bool isOn)
    {
        _isTorchOn = isOn;
    }
    void OnTorchSwitched(bool oldValue, bool newValue)
    {
        _torchLight.enabled = newValue;
    }
}
