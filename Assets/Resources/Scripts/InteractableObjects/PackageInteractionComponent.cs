using Mirror;
using UnityEngine;

public class PackageInteractionComponent : Interactable
{
    [SerializeField] private string _carriedLayer;
    [SerializeField] private string _droppedLayer;
    Rigidbody _rigidbody;
    bool _isCarried = false;
    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (isServer)
        {
            _rigidbody.isKinematic = false;
        }
        else
        {
            _rigidbody.isKinematic = true;
        }
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

        transform.SetParent(playerIdentity.transform);
        transform.localPosition = new Vector3(0f, 0f, 1f);
        transform.localRotation = Quaternion.identity;
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
        transform.SetParent(null);
        Debug.Log("Restoring physics layer and detaching from player");
    }
}
