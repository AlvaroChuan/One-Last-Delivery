using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NetRigidbodyController : NetworkBehaviour
{
    [SerializeField] private bool _assignHostOwnershipOnLoad = true;
    Rigidbody _rigidbody;
    public bool enableRigidbodyControl = true;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if(isServer && _assignHostOwnershipOnLoad)
        {
            _rigidbody.isKinematic = false; // Enable physics for the server/host
            netIdentity.AssignClientAuthority(NetworkServer.localConnection);
        }
        else
        {
            _rigidbody.isKinematic = true; // Disable physics on clients without authority
        }
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        if (!enableRigidbodyControl) return;

        _rigidbody.isKinematic = false; // Enable physics for the owning client
    }

    public override void OnStopAuthority()
    {
        base.OnStopAuthority();

        if (!enableRigidbodyControl) return;

        _rigidbody.isKinematic = true; // Disable physics when losing authority
    }
}