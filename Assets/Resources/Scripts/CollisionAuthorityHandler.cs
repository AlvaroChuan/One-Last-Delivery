using System.Collections;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetRigidbodyController))]
public class CollisionAuthorityHandler : NetworkBehaviour
{
    [SerializeField] int _priority = 0; // Objects with higher priority will keep authority in collisions with lower priority objects
    NetworkConnectionToClient _ownerConnection;
    Rigidbody _rigidbody;
    Collider _collider;

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
        }
    }

    IEnumerator Rubberband()
    {
        yield return new WaitForSeconds(0.5f); // Wait for a short duration to allow the collision to be processed
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

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        _collider.isTrigger = false; // Enable physics collisions for clients with authority
    }

    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        _collider.isTrigger = true; // Prevent physics collisions for clients without authority
    }
}
