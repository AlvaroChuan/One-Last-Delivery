using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NetRigidbodyController : NetworkBehaviour
{
    Rigidbody _rigidbody;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if(isServer)
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

        _rigidbody.isKinematic = false; // Enable physics for the owning client
    }

    public override void OnStopAuthority()
    {
        base.OnStopAuthority();

        _rigidbody.isKinematic = true; // Disable physics when losing authority
    }
}