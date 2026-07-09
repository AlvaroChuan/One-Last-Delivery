using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PackageTruckParentingHandler : NetworkBehaviour
{
    float _timeToWaitAfterDrop = 0.5f;
    Rigidbody _rigidbody;
    PackageCarryComponent _packageCarryComponent;
    BoxCollider _collider;
    RaycastHit[] _hitBuffer = new RaycastHit[10];
    int _supportingObjects = 0;
    bool _isBeingCarried = false;
    bool _isInTruck = false;
    [SyncVar(hook = nameof(OnParentChanged))] NetworkIdentity _parent;

    void Awake()
    {
        BoxCollider[] colliders = GetComponents<BoxCollider>();
        if (colliders.Length > 1)
        {
            foreach (var col in colliders)
            {
                if (!col.isTrigger)
                {
                    _collider = col;
                    break;
                }
            }
        }
        _rigidbody = GetComponent<Rigidbody>();
        _packageCarryComponent = GetComponent<PackageCarryComponent>();
        _packageCarryComponent.onStartCarrying += OnPackagePickup;
        _packageCarryComponent.onStopCarrying += OnPackageDrop;
    }
    void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;
        if (other.CompareTag("TruckInterior"))
        {
            _parent = other.GetComponentInParent<NetworkIdentity>();
            _isInTruck = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!isServer) return;
        if (other.CompareTag("TruckInterior"))
        {
            _parent = null;
            _isInTruck = false;
        }
    }

    void OnParentChanged(NetworkIdentity oldParent, NetworkIdentity newParent)
    {
        if (newParent != null)
        {
            transform.SetParent(newParent.transform, true);
            _isInTruck = true;
        }
        else
        {
            transform.SetParent(null, true);
            _isInTruck = false;
        }
        GetComponentInParent<NetworkTransformBase>().ResetState();
    }

    void OnPackagePickup()
    {
        _isBeingCarried = true;
        _rigidbody.isKinematic = false;
    }

    void OnPackageDrop()
    {
        Invoke(nameof(SetPackageDropped), _timeToWaitAfterDrop);
    }

    void SetPackageDropped()
    {
        _isBeingCarried = false;
    }

    void FixedUpdate()
    {
        if (!isOwned)
            return;
        if (!_rigidbody.isKinematic && _isInTruck && !_isBeingCarried && _rigidbody.linearVelocity.sqrMagnitude < 0.01f && _rigidbody.angularVelocity.sqrMagnitude < 0.01f)
        {
            Vector3 size = _collider.size;
            Vector3 center = _collider.center + transform.position;

            _supportingObjects = Physics.BoxCastNonAlloc(center, size / 2f, Vector3.down, _hitBuffer, transform.rotation, 0.1f, ~0, queryTriggerInteraction: QueryTriggerInteraction.Ignore);

            _rigidbody.isKinematic = true;
        }
        if (isOwned && _rigidbody.isKinematic)
        {
            CheckIfSupportRemoved();
        }
    }

    void CheckIfSupportRemoved()
    {
        Vector3 size = _collider.size;
        Vector3 center = _collider.center + transform.position;

        int currentSupportingObjects = Physics.BoxCastNonAlloc(center, size / 2f, Vector3.down, _hitBuffer, transform.rotation, 0.1f, ~0, queryTriggerInteraction: QueryTriggerInteraction.Ignore);

        if (currentSupportingObjects < _supportingObjects)
        {
            _rigidbody.isKinematic = false;
            _supportingObjects = 0;
        }
    }

    void OnDrawGizmos()
    {
        if (_collider == null) return;

        Gizmos.color = Color.red;
        Vector3 size = _collider.size;
        Vector3 center = _collider.center;
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(center, size);
    }
}