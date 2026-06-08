using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PackageInteractionComponent : Interactable
{
    [SerializeField] private string _carriedLayer = "Default";
    [SerializeField] private string _droppedLayer = "Interactables";
    PackageCarryComponent _carryComponent;
    Rigidbody _rigidbody;
    bool _interacted = false;
    bool _isCarried = false;

    void Awake()
    {
        _carryComponent = GetComponent<PackageCarryComponent>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    public override void OnStartServer()
    {
        netIdentity.AssignClientAuthority(NetworkServer.localConnection);
    }

    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer(_droppedLayer);
    }

    public override void ServerInteract(GameObject interactor)
    {
        NetworkIdentity interactorIdentity = interactor.GetComponent<NetworkIdentity>();

        gameObject.layer = LayerMask.NameToLayer(_carriedLayer);

        netIdentity.RemoveClientAuthority();
        netIdentity.AssignClientAuthority(interactorIdentity.connectionToClient);
    }

    public override void ClientInteraction(GameObject interactor)
    {
        _interacted = true;
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        if(!_interacted)
        {
            return; // Prevent grabbing logic from running on clients that gain authority without interacting (e.g. when the object is spawned and authority is assigned to the host)
        }
        _interacted = false; // Reset interaction flag for future interactions
        Grab();
    }

    public override void OnStopAuthority()
    {
        if(!_isCarried) return; // Prevent dropping logic from running if we lose authority without being carried (e.g. when the object is destroyed or authority is transferred to another client)
        DropFromPlayer(Vector3.zero);
    }

    void Grab()
    {
        NetworkIdentity playerIdentity = NetworkClient.connection.identity;

        PlayerInventoryComponent inventory = playerIdentity.gameObject.GetComponent<PlayerInventoryComponent>();
        inventory.SetSlotSelection(-1);
        inventory.SetCarryingPackage(this);

        _carryComponent.StartCarrying(playerIdentity.gameObject);

        _isCarried = true;
    }

    public void DropFromPlayer(Vector3 throwForce)
    {
        _isCarried = false;

        _carryComponent.StopCarrying();
        _rigidbody.AddForce(throwForce, ForceMode.Impulse);

        CmdDrop();
    }

    [Command(requiresAuthority = false)]
    void CmdDrop()
    {
        gameObject.layer = LayerMask.NameToLayer(_droppedLayer);
    }
}
