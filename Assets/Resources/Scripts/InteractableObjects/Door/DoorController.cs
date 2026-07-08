using UnityEngine;
using Mirror;

[RequireComponent(typeof(DoorInteractionComponent))]
[RequireComponent(typeof(EvilDoorInteractionComponent))]
public class DoorController : NetworkBehaviour
{
    DoorInteractionComponent _doorInteractionComponent;
    EvilDoorInteractionComponent _evilDoorInteractionComponent;

    void Awake()
    {
        _doorInteractionComponent = GetComponent<DoorInteractionComponent>();
        _evilDoorInteractionComponent = GetComponent<EvilDoorInteractionComponent>();
        _evilDoorInteractionComponent.enabled = false; // Start with the evil door interaction disabled
    }

    [Server]
    public void CorruptDoor()
    {
        _doorInteractionComponent.enabled = false;
        _evilDoorInteractionComponent.enabled = true;
        RpcCorruptDoor();
    }

    [ClientRpc]
    public void RpcCorruptDoor()
    {
        // Change the door's appearance to indicate it's corrupted (e.g., change material, play animation, etc.)
    }
}