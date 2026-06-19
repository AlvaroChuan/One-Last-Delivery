using UnityEngine;
using Mirror;
using UnityEngine.AI;
using System.Collections.Generic;
using Unity.VisualScripting;

[RequireComponent(typeof(PackageDistanceDetector))]
[RequireComponent(typeof(ChaseBehaviour))]
[RequireComponent(typeof(EnemyStunComponent))]
public class Swiper : NetworkBehaviour
{
    private enum SwiperState
    {
        Wandering,
        ChasingPackage,
        RunningToHideout
    }
    [SerializeField] private float _packageDetectionRadius = 10f; // Radius within which to detect packages
    [SerializeField] private float _packageDetectionInterval = 1f; // Interval to check for packages
    [SerializeField] private float _packageStealDistance = 1f; // Distance at which the Swiper can steal a package
    [SerializeField] private string _packageCarryLayer = "Enemies"; // Layer to assign to the package when carried
    [SerializeField] private Transform _packageCarryPoint; // Point where the package will be carried
    [SerializeField] private float _wanderInterval = 5f; // Interval to wander when no package is detected
    [SerializeField] private float _wanderSpeed = 3.5f;
    [SerializeField] private float _wanderAcceleration = 8f;
    [SerializeField] private float _wanderRadius = 50f; // Radius within which to wander
    [SerializeField] private float _wanderNavMeshSampleDistance = 5f; // Distance to sample the NavMesh for wandering
    [SerializeField] private int _wanderNavMeshSampleAttempts = 10; // Number of attempts to find a valid NavMesh position for wandering
    [SerializeField] private float _stealSpeed = 10f;
    [SerializeField] private float _stealAcceleration = 50f;
    [SerializeField] private GameObject _hideoutPrefab;
    [SerializeField] private float _hideoutRadius = 5f; // Radius around the hideout location to consider as "reached"
    private PackageDistanceDetector _packageDistanceDetector;
    private ChaseBehaviour _chaseBehaviour;
    private EnemyStunComponent _enemyStunComponent;
    private GameObject _targetPackage;
    float _packageDetectionTimer = 0f;
    float _wanderTimer = 0f;
    HashSet<GameObject> _excludedPackages = new HashSet<GameObject>();

    private SwiperState _currentState = SwiperState.Wandering;

    private float _packageStealDistanceSqr;
    private float _hideoutRadiusSqr;
    private GameObject _hideout;

    private void Awake()
    {
        _packageDistanceDetector = GetComponent<PackageDistanceDetector>();
        _chaseBehaviour = GetComponent<ChaseBehaviour>();
        _enemyStunComponent = GetComponent<EnemyStunComponent>();

        _packageStealDistanceSqr = _packageStealDistance * _packageStealDistance;
        _hideoutRadiusSqr = _hideoutRadius * _hideoutRadius;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (_hideoutPrefab != null)
        {
            GameObject hideoutInstance = Instantiate(_hideoutPrefab, transform.position, Quaternion.identity);
            NetworkServer.Spawn(hideoutInstance);
            _hideout = hideoutInstance;
        }
        else
        {
            Debug.LogError("Hideout Prefab is not assigned in the Swiper script.");
        }

        StartWander();
    }

    void OnEnable()
    {
        if (isServer)
        {
            _enemyStunComponent.onStunChangedEvent += OnStunStateChanged;
        }
    }
    void OnDisable()
    {
        if (isServer)
        {
            _enemyStunComponent.onStunChangedEvent -= OnStunStateChanged;
        }
    }

    void Update()
    {
        if (!isServer) return;

        if (_enemyStunComponent.IsStunned) return;

        if (_currentState == SwiperState.Wandering)
        {
            _packageDetectionTimer += Time.deltaTime;
            if (_packageDetectionTimer >= _packageDetectionInterval)
            {
                _packageDetectionTimer = 0f;
                if (CheckForPackage())
                {
                    StartSteal();
                    return;
                }
            }

            _wanderTimer += Time.deltaTime;
            if (_wanderTimer >= _wanderInterval)
            {
                _wanderTimer = 0f;
                StartWander();
            }
        }
        else if (_currentState == SwiperState.ChasingPackage)
        {
            if (_targetPackage == null)
            {
                StartWander();
                return;
            }

            float distanceToPackageSqr = (_targetPackage.transform.position - transform.position).sqrMagnitude;
            if (distanceToPackageSqr <= _packageStealDistanceSqr)
            {
                PickupPackage();
                StartRunToHideout();
            }
        }
        else if (_currentState == SwiperState.RunningToHideout)
        {
            if (_targetPackage == null)
            {
                StartWander();
                return;
            }
            _targetPackage.transform.position = _packageCarryPoint.position; // Keep the package at the carry point
            _targetPackage.transform.rotation = _packageCarryPoint.rotation; // Keep the package's rotation aligned with the carry point
            float distanceToHideoutSqr = (_hideout.transform.position - transform.position).sqrMagnitude;
            if (distanceToHideoutSqr <= _hideoutRadiusSqr)
            {
                DropPackage();
                StartWander();
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _packageDetectionRadius);
        if (_hideout != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_hideout.transform.position, _hideoutRadius);
        }
    }

    void OnStunStateChanged(EnemyStunComponent.StunChangeInfo stunInfo)
    {
        if (stunInfo.isStunned)
        {
            DropPackage();
        }
        else
        {
            StartWander();
        }
    }

    bool CheckForPackage()
    {
        _excludedPackages.Clear();
        do
        {
            _targetPackage = _packageDistanceDetector.DetectClosestNonCorruptedPackage(_packageDetectionRadius, _excludedPackages);
            if (_targetPackage != null)
            {
                Vector3 packagePosition = _targetPackage.transform.position;
                Vector3 hideoutPosition = _hideout.transform.position;
                packagePosition.y = 0f; // Ignore vertical distance
                hideoutPosition.y = 0f; // Ignore vertical distance
                if (Vector3.SqrMagnitude(packagePosition - hideoutPosition) <= _hideoutRadiusSqr)
                {
                    _excludedPackages.Add(_targetPackage);
                }
                else
                {
                    return true; // Found a valid package to chase
                }
            }
        } while (_targetPackage != null);
        return false;
    }

    void StartWander()
    {
        DevLogger.Log("Swiper is wandering.");
        _chaseBehaviour.SetSpeed(_wanderSpeed);
        _chaseBehaviour.SetAcceleration(_wanderAcceleration);
        Vector2 randomDirection;
        Vector3 wanderTarget;

        bool isOnNavMesh;

        int sampleAttempts = 0;
        do
        {
            randomDirection = Random.insideUnitCircle * _wanderRadius;
            wanderTarget = new Vector3(randomDirection.x, transform.position.y, randomDirection.y) + transform.position;
            _chaseBehaviour.SetTarget(wanderTarget, _wanderNavMeshSampleDistance, out isOnNavMesh);
            _chaseBehaviour.StartChasing();
            sampleAttempts++;
        } while (!isOnNavMesh && sampleAttempts < _wanderNavMeshSampleAttempts);

        DevLogger.Log($"Swiper wander target: {wanderTarget}, isOnNavMesh: {isOnNavMesh}, sampleAttempts: {sampleAttempts}");

        if (!isOnNavMesh)
        {
            _chaseBehaviour.StopChasing();
        }

        _currentState = SwiperState.Wandering;
    }

    void StartSteal()
    {
        DevLogger.Log($"Swiper is chasing package: {_targetPackage.name}");
        _chaseBehaviour.SetSpeed(_stealSpeed);
        _chaseBehaviour.SetAcceleration(_stealAcceleration);
        _chaseBehaviour.SetTarget(_targetPackage);
        _chaseBehaviour.StartChasing();
        _currentState = SwiperState.ChasingPackage;
    }

    void StartRunToHideout()
    {
        DevLogger.Log("Swiper is running to hideout.");
        _chaseBehaviour.SetSpeed(_stealSpeed);
        _chaseBehaviour.SetTarget(_hideout);
        _chaseBehaviour.StartChasing();
        _currentState = SwiperState.RunningToHideout;
    }

    void DropPackage()
    {
        if (_targetPackage == null)
        {
            DevLogger.LogError("Attempted to drop a package, but _targetPackage is null.");
            return;
        }

        NetworkIdentity packageNetIdentity = _targetPackage.GetComponent<NetworkIdentity>();
        if (packageNetIdentity == null)
        {
            DevLogger.LogError("Package does not have a NetworkIdentity component.");
            return;
        }

        _chaseBehaviour.StopChasing();

        _targetPackage.layer = LayerMask.NameToLayer(_targetPackage.GetComponent<PackageInteractionComponent>().DroppedLayer);
        Rigidbody packageRigidbody = _targetPackage.GetComponent<Rigidbody>();
        packageRigidbody.isKinematic = false; // Make the package non-kinematic so it can be dropped
        _targetPackage.GetComponent<CollisionAuthorityHandler>().enableAuthoritySwap = true; // Re-enable authority swapping when dropped
        _targetPackage.GetComponent<NetRigidbodyController>().enableRigidbodyControl = true; // Re-enable Rigidbody control for the package when dropped
    }
    void PickupPackage()
    {
        if (_targetPackage == null)
        {
            DevLogger.LogError("Attempted to pick up a package, but _targetPackage is null.");
            return;
        }

        NetworkIdentity packageNetIdentity = _targetPackage.GetComponent<NetworkIdentity>();
        if (packageNetIdentity == null)
        {
            DevLogger.LogError("Package does not have a NetworkIdentity component.");
            return;
        }

        packageNetIdentity.RemoveClientAuthority();
        packageNetIdentity.AssignClientAuthority(NetworkServer.localConnection);
        _targetPackage.layer = LayerMask.NameToLayer(_packageCarryLayer);
        _targetPackage.GetComponent<NetRigidbodyController>().enableRigidbodyControl = false; // Disable Rigidbody control for the package while being carried
        _targetPackage.GetComponent<CollisionAuthorityHandler>().enableAuthoritySwap = false; // Disable authority swapping for the package while being carried
        _targetPackage.GetComponent<Rigidbody>().isKinematic = true; // Make the package kinematic so it can be carried
        _targetPackage.transform.position = _packageCarryPoint.position; // Move the package to the carry point
    }
}