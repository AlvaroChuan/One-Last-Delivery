using System.Collections;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(NetRigidbodyController))]
public class CollisionAuthorityHandler : NetworkBehaviour
{
    [SerializeField] int _priority = 0; // Objects with higher priority will keep authority in collisions with lower priority objects
    NetworkConnectionToClient _ownerConnection;
    Rigidbody _rigidbody;
    Collider _collider;

    float _rubberbandDuration = 0.5f; // Duration to wait before rubberbanding back if authority isn't gained
    float _timeBetweenRequests = 0.5f; // Minimum time between authority requests to prevent spamming
    bool _canRequestAuthority = true;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (isServer)
        {
            _ownerConnection = NetworkServer.localConnection;
            _collider.isTrigger = false; // Enable physics collisions for the server/host
        }
        else
        {
            _collider.isTrigger = true; // Prevent physics collisions on clients without authority
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if(!_canRequestAuthority) return;

        if(other.TryGetComponent(out CollisionAuthorityHandler otherHandler))
        {
            if (otherHandler._priority < _priority)
            {
                Debug.Log("Collision detected with lower priority object. Keeping authority.");
                return;
            }
        }

        ClientPrediction(other);
        if (isServer)
        {
            ServerCollisionHandshake(other.gameObject);
        }
    }

    void ClientPrediction(Collider other)
    {
        NetworkIdentity otherIdentity = other.GetComponent<NetworkIdentity>();
        if (otherIdentity == null)
        {
            otherIdentity = other.GetComponentInParent<NetworkIdentity>();
        }

        if (otherIdentity == null)
        {
            return;
        }

        if(otherIdentity.isOwned && !isOwned)
        {
            Debug.Log("Client-side collision detected with local player. Predicting authority change.");
            _rigidbody.isKinematic = false;
            _collider.isTrigger = false; // Enable physics collisions for prediction
            StartCoroutine(Rubberband());
            StartCoroutine(AuthorityRequestCooldown());
        }
    }

    IEnumerator Rubberband()
    {
        yield return new WaitForSeconds(_rubberbandDuration); // Wait for a short duration to allow the collision to be processed
        if (!isOwned)
        {
            _rigidbody.isKinematic = true;
            _collider.isTrigger = true; // Disable physics collisions if we didn't gain authority
        }
    }

    void ServerCollisionHandshake(GameObject collider)
    {
        NetworkIdentity otherIdentity = collider.GetComponent<NetworkIdentity>();
        if (otherIdentity == null)
        {
            otherIdentity = collider.GetComponentInParent<NetworkIdentity>();
        }

        if (otherIdentity != null)
        {
            if (otherIdentity.connectionToClient != _ownerConnection)
            {
                _ownerConnection = otherIdentity.connectionToClient;
                netIdentity.RemoveClientAuthority();
                netIdentity.AssignClientAuthority(otherIdentity.connectionToClient);
            }
        }
    }

    IEnumerator AuthorityRequestCooldown()
    {
        _canRequestAuthority = false;
        yield return new WaitForSeconds(_timeBetweenRequests);
        _canRequestAuthority = true;
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        _collider.isTrigger = false; // Enable physics collisions for clients with authority
    }

    public override void OnStopAuthority()
    {
        if (!NetworkClient.active) return;
        base.OnStopAuthority();
        _collider.isTrigger = true; // Prevent physics collisions for clients without authority
        CmdRestoreVelocity(_rigidbody.linearVelocity, _rigidbody.angularVelocity); // Restore velocity on all clients to prevent rubberbanding
    }

    [Command(requiresAuthority = false)]
    public void CmdRestoreVelocity(Vector3 velocity, Vector3 angularVelocity)
    {
        RpcRestoreVelocity(velocity, angularVelocity);
    }
    [ClientRpc]
    public void RpcRestoreVelocity(Vector3 velocity, Vector3 angularVelocity)
    {
        if (!isOwned) return;

        _rigidbody.linearVelocity = velocity;
        _rigidbody.angularVelocity = angularVelocity;
    }
}
