using UnityEngine;
using Mirror;

[RequireComponent(typeof(NavMeshMovementComponent))]
[RequireComponent(typeof(EnemyAttackComponent))]
[RequireComponent(typeof(EnemyStunComponent))]
[RequireComponent(typeof(WanderBehaviour))]
[RequireComponent(typeof(PlayerChaseBehaviour))]
public class BasicEnemy : NetworkBehaviour
{
    [SerializeField] private float _playerDetectionRadius = 10f; // Radius within which to detect players
    private NavMeshMovementComponent _movementComponent;
    private EnemyAttackComponent _attackComponent;
    private EnemyStunComponent _enemyStunComponent;
    private WanderBehaviour _wanderBehaviour;
    private PlayerChaseBehaviour _playerChaseBehaviour;

    void Awake()
    {
        _movementComponent = GetComponent<NavMeshMovementComponent>();
        _wanderBehaviour = GetComponent<WanderBehaviour>();
        _attackComponent = GetComponent<EnemyAttackComponent>();
        _enemyStunComponent = GetComponent<EnemyStunComponent>();
        _playerChaseBehaviour = GetComponent<PlayerChaseBehaviour>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        _attackComponent.onAttackStartedEvent += OnAttackStarted;
        _enemyStunComponent.onStunChangedEvent += OnStunChanged;

        _wanderBehaviour.StartWander(); // Start wandering when the enemy is spawned
    }

    void OnEnable()
    {
        _attackComponent.onAttackStartedEvent += OnAttackStarted;
        _enemyStunComponent.onStunChangedEvent += OnStunChanged;
    }

    void OnDisable()
    {
        _attackComponent.onAttackStartedEvent -= OnAttackStarted;
        _enemyStunComponent.onStunChangedEvent -= OnStunChanged;
    }

    void Update()
    {
        if (!isServer) return;

        if (_attackComponent.IsAttacking) return;

        if (_enemyStunComponent.IsStunned) return;

        _playerChaseBehaviour.UpdateChase(Time.deltaTime, _playerDetectionRadius);

        if (_playerChaseBehaviour.IsChasing)
        {
            _attackComponent.TryAttackIfInRange(_playerChaseBehaviour.CurrentTarget);
        }
        else
        {
            _wanderBehaviour.UpdateWander(Time.deltaTime);
        }
    }

    void OnAttackStarted()
    {
        if (!isServer) return;

        _movementComponent.SetTarget(null); // Clear target when attack starts
    }

    void OnStunChanged(EnemyStunComponent.StunChangeInfo stunInfo)
    {
        if (!isServer) return;

        if (stunInfo.isStunned)
        {
            _movementComponent.SetTarget(null); // Clear target when stunned
        }
    }
}