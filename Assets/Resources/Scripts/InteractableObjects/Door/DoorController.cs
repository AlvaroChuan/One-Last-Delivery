using UnityEngine;
using Mirror;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(DoorInteractionComponent))]
[RequireComponent(typeof(EvilDoorInteractionComponent))]
public class DoorController : NetworkBehaviour
{
    DoorInteractionComponent _doorInteractionComponent;
    EvilDoorInteractionComponent _evilDoorInteractionComponent;
    [SerializeField] private DecalProjector _corruptionDecal;

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
        _corruptionDecal.enabled = true;
    }
}