using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PackageInteractionComponent : Interactable
{
    [SerializeField] private string _carriedLayer = "Default";
    [SerializeField] private string _droppedLayer = "Interactables";
    PackageCarryComponent _carryComponent;
    Rigidbody _rigidbody;
    bool _isGrabbedByHost = false;
    Vector3 _throwForce = new Vector3(0f, 0f, 0f);
    bool _interacted = false;

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

    public override void Interact(GameObject interactor)
    {
        NetworkIdentity interactorIdentity = interactor.GetComponent<NetworkIdentity>();

        gameObject.layer = LayerMask.NameToLayer(_carriedLayer);

        _isGrabbedByHost = interactorIdentity.isLocalPlayer;

        netIdentity.RemoveClientAuthority();
        netIdentity.AssignClientAuthority(interactorIdentity.connectionToClient);
    }

    public override void LocalInteraction(GameObject interactor)
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

        _rigidbody.isKinematic = false;

        if (isServer)
        {
            _rigidbody.AddForce(_throwForce, ForceMode.VelocityChange);
            _throwForce = Vector3.zero; // Reset throw force after applying it on the server
            if(!_isGrabbedByHost)
            {
                return; // Skip grabbing logic on the initial authority assignment when the object is spawned
            }
        }

        Grab();
    }

    void Grab()
    {

        NetworkIdentity playerIdentity = NetworkClient.connection.identity;

        PlayerInventoryComponent inventory = playerIdentity.gameObject.GetComponent<PlayerInventoryComponent>();
        inventory.SetSlotSelection(-1);
        inventory.SetCarryingPackage(this);

        _carryComponent.StartCarrying(playerIdentity.gameObject);
    }

    public void DropFromPlayer(Vector3 throwForce)
    {
        if (!isOwned) return;

        _carryComponent.StopCarrying();

        CmdDrop(throwForce);
    }

    [Command(requiresAuthority = false)]
    void CmdDrop(Vector3 throwForce)
    {
        _isGrabbedByHost = false;
        gameObject.layer = LayerMask.NameToLayer(_droppedLayer);
        _throwForce = throwForce;
        netIdentity.RemoveClientAuthority();
        netIdentity.AssignClientAuthority(NetworkServer.localConnection);
    }
}
