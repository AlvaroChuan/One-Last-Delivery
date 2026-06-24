using Mirror;
using UnityEngine;

[RequireComponent(typeof(CollisionAuthorityHandler))]
[RequireComponent(typeof(Rigidbody))]
public class PackageInteractionComponent : Interactable
{
    [SerializeField] private string _carriedLayer = "Default";
    [SerializeField] private string _droppedLayer = "Interactables";
    public string CarriedLayer => _carriedLayer;
    public string DroppedLayer => _droppedLayer;
    PackageCarryComponent _carryComponent;
    CollisionAuthorityHandler _collisionAuthorityHandler;
    Rigidbody _rigidbody;
    bool _interacted = false;
    bool _isCarried = false;

    void Awake()
    {
        _carryComponent = GetComponent<PackageCarryComponent>();
        _collisionAuthorityHandler = GetComponent<CollisionAuthorityHandler>();
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

        ServerSetLayer(_carriedLayer);

        netIdentity.RemoveClientAuthority();
        netIdentity.AssignClientAuthority(interactorIdentity.connectionToClient);

        _collisionAuthorityHandler.enableAuthoritySwap = false; // Disable authority swapping while being carried
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
        inventory.SetCarryingPackage(gameObject);

        _carryComponent.StartCarrying(playerIdentity.gameObject);

        _isCarried = true;
    }

    public void DropFromPlayer(Vector3 throwForce)
    {
        if (isServer)
        {
            ServerSetLayer(_droppedLayer);
        }
        else
        {
            CmdSetLayer(_droppedLayer);
        }

        _isCarried = false;

        _carryComponent.StopCarrying();
        _rigidbody.AddForce(throwForce, ForceMode.Impulse);
    }

    [Server]
    void ServerSetLayer(string layerName)
    {
        if (layerName == _droppedLayer)
        {
            if (gameObject.layer != LayerMask.NameToLayer(_carriedLayer))
            {
                return; // Prevent dropping logic from running if the package is not currently being carried
            }
            _collisionAuthorityHandler.enableAuthoritySwap = true; // Re-enable authority swapping when dropped
        }
        gameObject.layer = LayerMask.NameToLayer(layerName);
        RpcSetLayer(layerName);
    }
    [Command(requiresAuthority = false)]
    void CmdSetLayer(string layerName)
    {
        ServerSetLayer(layerName);
        RpcSetLayer(layerName);
    }
    [ClientRpc]
    void RpcSetLayer(string layerName)
    {
        gameObject.layer = LayerMask.NameToLayer(layerName);
    }
}
