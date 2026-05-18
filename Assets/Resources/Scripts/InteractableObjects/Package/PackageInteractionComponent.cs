using Mirror;
using UnityEngine;

public class PackageInteractionComponent : Interactable
{
    [SerializeField] private string _carriedLayer = "Default";
    [SerializeField] private string _droppedLayer = "Interactables";
    [SerializeField] private Vector3 _offsetFromPlayer = new Vector3(0f, -.25f, 1f);
    Rigidbody _rigidbody;

    NetworkTransformReliable _networkTransform;

    Collider _packageCollider;
    Collider _playerCollider;

    bool _isCarried = false;
    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _packageCollider = GetComponent<Collider>();
        _networkTransform = GetComponent<NetworkTransformReliable>();
    }

    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer(_droppedLayer);
    }

    public override void Interact(GameObject interactor)
    {
        if (_isCarried) return;

        NetworkIdentity interactorIdentity = interactor.GetComponent<NetworkIdentity>();

        netIdentity.AssignClientAuthority(interactorIdentity.connectionToClient);
        _isCarried = true;
        _rigidbody.isKinematic = true;
        gameObject.layer = LayerMask.NameToLayer(_carriedLayer);
        RpcAttachToPlayer(interactorIdentity);
    }

    [ClientRpc]
    public void RpcAttachToPlayer(NetworkIdentity playerIdentity)
    {
        PlayerInventoryComponent inventory = playerIdentity.gameObject.GetComponent<PlayerInventoryComponent>();
        inventory.SetSlotSelection(-1);
        inventory.SetCarryingPackage(this);

        gameObject.layer = LayerMask.NameToLayer(_carriedLayer);

        _networkTransform.clientSnapshots.Clear();
        _networkTransform.serverSnapshots.Clear();

        transform.SetParent(playerIdentity.transform);
        transform.localPosition = _offsetFromPlayer;
        transform.localRotation = Quaternion.identity;

        _playerCollider = playerIdentity.GetComponent<Collider>();
        if (_playerCollider != null)
        {
            Physics.IgnoreCollision(_packageCollider, _playerCollider, true);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdDropFromPlayer(Vector3 throwForce)
    {
        netIdentity.RemoveClientAuthority();
        _isCarried = false;
        gameObject.layer = LayerMask.NameToLayer(_droppedLayer);

        _rigidbody.isKinematic = false;
        _rigidbody.AddForce(throwForce, ForceMode.VelocityChange);

        RpcDropFromPlayer();
    }

    [ClientRpc]
    public void RpcDropFromPlayer()
    {
        gameObject.layer = LayerMask.NameToLayer(_droppedLayer);
        _networkTransform.clientSnapshots.Clear();
        _networkTransform.serverSnapshots.Clear();
        transform.SetParent(null);

        if (_playerCollider != null)
        {
            Physics.IgnoreCollision(_packageCollider, _playerCollider, false);
        }

        _playerCollider = null;

        Debug.Log("Restoring physics layer and detaching from player");
    }
}
