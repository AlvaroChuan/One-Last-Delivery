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

        _attackComponent.onAttackEndedEvent += OnAttackEnded;
        _attackComponent.onAttackStartedEvent += OnAttackStarted;
        _enemyStunComponent.onStunChangedEvent += OnStunChanged;

        _wanderBehaviour.StartWander(); // Start wandering when the enemy is spawned
    }

    void OnEnable()
    {
        _attackComponent.onAttackEndedEvent += OnAttackEnded;
        _attackComponent.onAttackStartedEvent += OnAttackStarted;
        _enemyStunComponent.onStunChangedEvent += OnStunChanged;
    }

    void OnDisable()
    {
        _attackComponent.onAttackEndedEvent -= OnAttackEnded;
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

        _movementComponent.CanMove = false; // Stop moving when attack starts
    }
    void OnAttackEnded()
    {
        if (!isServer) return;

        _movementComponent.CanMove = true; // Resume moving after attack ends
        _playerChaseBehaviour.CheckForPlayer(_playerDetectionRadius); // Recheck for players after attack
    }

    void OnStunChanged(EnemyStunComponent.StunChangeInfo stunInfo)
    {
        if (!isServer) return;

        if (stunInfo.isStunned)
        {
            _movementComponent.CanMove = false; // Stop moving when stunned
        }
        else
        {
            _movementComponent.CanMove = true; // Resume moving when not stunned
            _playerChaseBehaviour.CheckForPlayer(_playerDetectionRadius); // Recheck for players after stun ends
        }
    }
}