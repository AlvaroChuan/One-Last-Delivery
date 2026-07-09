using UnityEngine;
using Mirror;
using UnityEngine.AI;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;

[RequireComponent(typeof(PackageDistanceDetector))]
[RequireComponent(typeof(NavMeshMovementComponent))]
[RequireComponent(typeof(EnemyStunComponent))]
[RequireComponent(typeof(WanderBehaviour))]
[RequireComponent(typeof(NavMeshAgent))]
public class Swiper : NetworkBehaviour
{
    private enum SwiperState
    {
        Wandering,
        ChasingPackage,
        RunningToHideout
    }

    [Header("Detection Settings")]
    [SerializeField] private float _packageDetectionRadius = 10f;
    [SerializeField] private float _packageDetectionInterval = 1f;
    [SerializeField] private float _packageStealDistance = 1f;

    [Header("Carrying Settings")]
    [SerializeField] private string _packageCarryLayer = "Enemies";
    [SerializeField] private string _packageDefaultLayer = "Interactables";
    [SerializeField] private Transform _packageCarryPoint;

    [Header("Movement Settings")]
    [SerializeField] private float _stealSpeed = 10f;
    [SerializeField] private float _stealAcceleration = 50f;

    [Header("Hideout Settings")]
    [SerializeField] private GameObject _hideoutPrefab;
    [SerializeField] private float _hideoutRadius = 5f;

    [Header("Animation Settings")]
    [SerializeField] private Animator _animator;

    private PackageDistanceDetector _packageDistanceDetector;
    private NavMeshMovementComponent _chaseBehaviour;
    private EnemyStunComponent _enemyStunComponent;
    private WanderBehaviour _wanderBehaviour;
    private NavMeshAgent _navMeshAgent;

    private GameObject _targetPackage;
    private float _packageDetectionTimer = 0f;
    private HashSet<GameObject> _excludedPackages = new HashSet<GameObject>();

    private SwiperState _currentState = SwiperState.Wandering;

    private float _packageStealDistanceSqr;
    private float _hideoutRadiusSqr;
    private GameObject _hideout;

    private void Awake()
    {
        _packageDistanceDetector = GetComponent<PackageDistanceDetector>();
        _chaseBehaviour = GetComponent<NavMeshMovementComponent>();
        _enemyStunComponent = GetComponent<EnemyStunComponent>();
        _wanderBehaviour = GetComponent<WanderBehaviour>();
        _navMeshAgent = GetComponent<NavMeshAgent>();

        _packageStealDistanceSqr = _packageStealDistance * _packageStealDistance;
        _hideoutRadiusSqr = _hideoutRadius * _hideoutRadius;

        _packageDetectionTimer = Random.Range(0f, _packageDetectionInterval);
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
        _enemyStunComponent.onStunChangedEvent += OnStunStateChanged;
    }

    void OnDisable()
    {
        _enemyStunComponent.onStunChangedEvent -= OnStunStateChanged;
    }

    void Update()
    {
        if (!isServer) return;

        UpdateAnimations();

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
            _wanderBehaviour.UpdateWander(Time.deltaTime);
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
            _targetPackage.transform.position = _packageCarryPoint.position;
            _targetPackage.transform.rotation = _packageCarryPoint.rotation;
            float distanceToHideoutSqr = (_hideout.transform.position - transform.position).sqrMagnitude;
            if (distanceToHideoutSqr <= _hideoutRadiusSqr * 0.5f)
            {
                DropPackage();
                StartWander();
            }
        }
    }

    private void UpdateAnimations()
    {
        if (_animator == null || _navMeshAgent == null) return;

        bool isMoving = _navMeshAgent.velocity.sqrMagnitude > 0.01f;

        bool isWalking = false;
        bool isRunning = false;
        bool hasPackage = false;

        if (!_enemyStunComponent.IsStunned && isMoving)
        {
            switch (_currentState)
            {
                case SwiperState.Wandering:
                    isWalking = true;
                    break;
                case SwiperState.ChasingPackage:
                    isRunning = true;
                    break;
                case SwiperState.RunningToHideout:
                    isRunning = true;
                    hasPackage = true;
                    break;
            }
        }

        _animator.SetBool("IsWalking", isWalking);
        _animator.SetBool("IsRunning", isRunning);
        _animator.SetBool("HasPackage", hasPackage);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (_hideout != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_hideout.transform.position, _hideoutRadius);
        }
    }
#endif

    void OnStunStateChanged(EnemyStunComponent.StunChangeInfo stunInfo)
    {
        if (!isServer) return;

        if (stunInfo.isStunned)
        {
            _chaseBehaviour.SetTarget(null);
            DropPackage();

            if (_animator != null)
            {
                _animator.SetBool("IsStunned", true);
            }
        }
        else
        {
            if (_animator != null)
            {
                _animator.SetBool("IsStunned", false);
            }
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
                packagePosition.y = 0f;
                hideoutPosition.y = 0f;
                if (Vector3.SqrMagnitude(packagePosition - hideoutPosition) <= _hideoutRadiusSqr)
                {
                    _excludedPackages.Add(_targetPackage);
                }
                else
                {
                    return true;
                }
            }
        } while (_targetPackage != null);
        return false;
    }

    void StartSteal()
    {
        _chaseBehaviour.SetSpeed(_stealSpeed);
        _chaseBehaviour.SetAcceleration(_stealAcceleration);
        _chaseBehaviour.SetTarget(_targetPackage);
        _currentState = SwiperState.ChasingPackage;
    }

    void StartRunToHideout()
    {
        _chaseBehaviour.SetSpeed(_stealSpeed);
        _chaseBehaviour.SetTarget(_hideout);
        _currentState = SwiperState.RunningToHideout;
    }

    void DropPackage()
    {
        if (_targetPackage == null)
        {
            return;
        }

        NetworkIdentity packageNetIdentity = _targetPackage.GetComponent<NetworkIdentity>();
        if (packageNetIdentity == null)
        {
            DevLogger.LogError("Package does not have a NetworkIdentity component.");
            return;
        }

        _chaseBehaviour.SetTarget(null);

        _targetPackage.GetComponent<PackageTruckParentingHandler>().CmdChangeLayer(LayerMask.NameToLayer(_packageDefaultLayer));
        Rigidbody packageRigidbody = _targetPackage.GetComponent<Rigidbody>();
        packageRigidbody.isKinematic = false;
        _targetPackage.GetComponent<CollisionAuthorityHandler>().enableAuthoritySwap = true;
        _targetPackage.GetComponent<NetRigidbodyController>().enableRigidbodyControl = true;

        Collider[] packageColliders = _targetPackage.GetComponentsInChildren<Collider>();
        foreach (var collider in packageColliders)
        {
            collider.enabled = true;
        }

        StartCoroutine(ReenablePackageDamage(_targetPackage.GetComponent<PackageHealthComponent>(), 0.5f));

        _targetPackage = null;
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
        _targetPackage.GetComponent<PackageTruckParentingHandler>().CmdChangeLayer(LayerMask.NameToLayer(_packageCarryLayer));
        _targetPackage.GetComponent<NetRigidbodyController>().enableRigidbodyControl = false;
        _targetPackage.GetComponent<CollisionAuthorityHandler>().enableAuthoritySwap = false;
        _targetPackage.GetComponent<Rigidbody>().isKinematic = true;
        _targetPackage.transform.position = _packageCarryPoint.position;

        Collider[] packageColliders = _targetPackage.GetComponentsInChildren<Collider>();
        foreach (var collider in packageColliders)
        {
            collider.enabled = false;
        }

        _targetPackage.GetComponent<PackageHealthComponent>().CanTakeDamage = false;
    }

    void StartWander()
    {
        _currentState = SwiperState.Wandering;
        _wanderBehaviour.StartWander();
    }

    IEnumerator ReenablePackageDamage(PackageHealthComponent packageHealthComponent, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (packageHealthComponent != null)
        {
            packageHealthComponent.CanTakeDamage = true;
        }
    }
}