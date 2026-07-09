using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PackageTruckParentingHandler : NetworkBehaviour
{
    struct PackageParentingData
    {
        public NetworkIdentity parent;
        public Vector3 worldPosition;
    }
    float _timeToWaitAfterDrop = 0.5f;
    Rigidbody _rigidbody;
    PackageCarryComponent _packageCarryComponent;
    BoxCollider _collider;
    RaycastHit[] _hitBuffer = new RaycastHit[10];
    int _supportingObjects = 0;
    bool _isBeingCarried = false;
    bool _isInTruck = false;
    [SyncVar(hook = nameof(OnParentChanged))] PackageParentingData _parent;
    [SyncVar(hook = nameof(OnWorldPositionChanged))] Vector3 _worldPosition;

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
        _isInTruck = true;
        if (!isServer) return;
        if (other.CompareTag("TruckInterior"))
        {
            _parent = new PackageParentingData
            {
                parent = other.GetComponentInParent<NetworkIdentity>(),
                worldPosition = transform.position
            };
        }
    }

    void OnTriggerExit(Collider other)
    {
        _isInTruck = false;
        if (!isServer) return;
        if (other.CompareTag("TruckInterior"))
        {
            _parent = new PackageParentingData
            {
                parent = null,
                worldPosition = transform.position
            };
        }
    }

    void OnParentChanged(PackageParentingData oldParent, PackageParentingData newParent)
    {
        if (newParent.parent != null)
        {
            transform.SetParent(newParent.parent.transform, true);
            _isInTruck = true;
            GetComponentInParent<NetworkTransformBase>().enabled = false; // Disable the NetworkTransformBase component on the parent to prevent conflicts
            transform.position = newParent.worldPosition; // Maintain the world position when changing parent
        }
        else
        {
            transform.SetParent(null, true);
            _isInTruck = false;
            GetComponentInParent<NetworkTransformBase>().enabled = true; // Re-enable the NetworkTransformBase component on the parent
            transform.position = newParent.worldPosition; // Maintain the world position when changing parent
        }
        GetComponentInParent<NetworkTransformBase>().ResetState(); // Reset the state of the NetworkTransformBase component to ensure proper synchronization
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

    void Update()
    {
        if(!isOwned) return;
        if (!_isBeingCarried) return;
        if (_parent.parent != null)
        {
            CmdSetWorldPosition(transform.position);
        }
    }

    [Command]
    void CmdSetWorldPosition(Vector3 position)
    {
        _worldPosition = position;
    }

    void OnWorldPositionChanged(Vector3 oldPosition, Vector3 newPosition)
    {
        transform.position = newPosition;
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