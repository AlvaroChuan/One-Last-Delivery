using System.Collections;
using Mirror;
using UnityEngine;

public class CollisionAuthorityHandler : NetworkBehaviour
{
    NetworkConnectionToClient _ownerConnection;
    Rigidbody _rigidbody;
    bool _touching;

    WaitForSeconds _touchCheckDelay = new WaitForSeconds(0.5f);
    Coroutine _touchCheckCoroutine;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (isServer)
        {
            netIdentity.AssignClientAuthority(NetworkServer.localConnection);
            _ownerConnection = NetworkServer.localConnection;
            _rigidbody.isKinematic = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isServer) return;

        NetworkIdentity otherIdentity = collision.collider.GetComponentInParent<NetworkIdentity>();
        Debug.Log("Collided with " + collision.collider.name);
        Debug.Log("otherIdentity != null: " + (otherIdentity != null));
        Debug.Log("otherIdentity.connectionToClient != _ownerConnection: " + (otherIdentity != null ? (otherIdentity.connectionToClient != _ownerConnection).ToString() : "N/A"));
        Debug.Log("_touching: " + _touching);

        if (otherIdentity != null && otherIdentity.connectionToClient != _ownerConnection && !_touching)
        {
            if (_touchCheckCoroutine != null)
            {
                StopCoroutine(_touchCheckCoroutine);
                _touchCheckCoroutine = null;
            }
            _touching = true;
            _ownerConnection = otherIdentity.connectionToClient;
            CmdGrantAuthority(otherIdentity);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (!isServer) return;

        NetworkIdentity otherIdentity = collision.collider.GetComponentInParent<NetworkIdentity>();
        if (otherIdentity != null && otherIdentity.connectionToClient != null && otherIdentity.connectionToClient == _ownerConnection)
        {
            _touchCheckCoroutine = StartCoroutine(TouchCheckCoroutine());
        }
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        GetComponent<NetworkTransformBase>().ResetState();
        _rigidbody.isKinematic = false;
    }

    [Command(requiresAuthority = false)]
    void CmdGrantAuthority(NetworkIdentity identity)
    {
        if(identity != null && identity.connectionToClient != null)
        {
            netIdentity.RemoveClientAuthority();
            netIdentity.AssignClientAuthority(identity.connectionToClient);
        }
    }

    IEnumerator TouchCheckCoroutine()
    {
        yield return _touchCheckDelay;
        _touching = false;
    }
}
