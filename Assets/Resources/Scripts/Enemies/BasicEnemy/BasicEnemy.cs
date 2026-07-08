using UnityEngine;
using Mirror;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshMovementComponent))]
[RequireComponent(typeof(EnemyAttackComponent))]
[RequireComponent(typeof(EnemyStunComponent))]
[RequireComponent(typeof(WanderBehaviour))]
[RequireComponent(typeof(PlayerChaseBehaviour))]
public class BasicEnemy : NetworkBehaviour
{
    [SerializeField] private float _playerDetectionRadius = 10f;
    [SerializeField] private Animator _animator;
    private NetworkAnimator _networkAnimator;
    private NavMeshMovementComponent _movementComponent;
    private EnemyAttackComponent _attackComponent;
    private EnemyStunComponent _enemyStunComponent;
    private WanderBehaviour _wanderBehaviour;
    private PlayerChaseBehaviour _playerChaseBehaviour;
    private NavMeshAgent _navMeshAgent;

    void Awake()
    {
        _networkAnimator = GetComponent<NetworkAnimator>();
        _movementComponent = GetComponent<NavMeshMovementComponent>();
        _wanderBehaviour = GetComponent<WanderBehaviour>();
        _attackComponent = GetComponent<EnemyAttackComponent>();
        _enemyStunComponent = GetComponent<EnemyStunComponent>();
        _playerChaseBehaviour = GetComponent<PlayerChaseBehaviour>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        _attackComponent.onAttackStartedEvent += OnAttackStarted;
        _enemyStunComponent.onStunChangedEvent += OnStunChanged;

        _wanderBehaviour.StartWander();
    }

    void OnDisable()
    {
        _attackComponent.onAttackStartedEvent -= OnAttackStarted;
        _enemyStunComponent.onStunChangedEvent -= OnStunChanged;
    }

    void Update()
    {
        if (!isServer) return;

        UpdateAnimations();

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

    private void UpdateAnimations()
    {
        if (_animator == null || _navMeshAgent == null) return;

        bool isMoving = _navMeshAgent.velocity.sqrMagnitude > 0.01f;

        if (_enemyStunComponent.IsStunned || _attackComponent.IsAttacking)
        {
            isMoving = false;
        }

        _animator.SetBool("IsWalking", isMoving);
    }

    void OnAttackStarted()
    {
        if (!isServer) return;

        _movementComponent.SetTarget(null);

        if (_animator != null)
        {
            _networkAnimator.SetTrigger("Attack");
        }
    }

    void OnStunChanged(EnemyStunComponent.StunChangeInfo stunInfo)
    {
        if (!isServer) return;

        if (stunInfo.isStunned)
        {
            _movementComponent.SetTarget(null);

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
        }

        if (_attackComponent.IsAttacking)
        {
            _attackComponent.CancelAttack();
        }
    }
}