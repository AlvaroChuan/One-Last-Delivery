using UnityEngine;
using Mirror;

[RequireComponent(typeof(ChaseBehaviour))]
[RequireComponent(typeof(AttackComponent))]
[RequireComponent(typeof(PlayerDistanceDetector))]
[RequireComponent(typeof(EnemyStunComponent))]
public class BasicEnemy : NetworkBehaviour
{
    [SerializeField] private float _playerRecheckInterval = 1f; // Interval to check for players
    private float _playerRecheckTimer = 0f;
    protected ChaseBehaviour _chaseBehaviour;
    private AttackComponent _attackComponent;
    private PlayerDistanceDetector _playerDistanceDetector;
    private EnemyStunComponent _enemyStunComponent;
    private bool _isAttacking = false;
    private bool _isStunned = false;
    private GameObject _currentTarget;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _chaseBehaviour = GetComponent<ChaseBehaviour>();
        _attackComponent = GetComponent<AttackComponent>();
        _playerDistanceDetector = GetComponent<PlayerDistanceDetector>();
        _enemyStunComponent = GetComponent<EnemyStunComponent>();
        _playerRecheckTimer = Random.Range(0f, _playerRecheckInterval); // Randomize initial timer to avoid synchronized checks
    }

    void OnEnable()
    {
        if (isServer)
        {
            _attackComponent.onAttackEndedEvent += OnAttackEnded;
            _attackComponent.onAttackStartedEvent += OnAttackStarted;
            _enemyStunComponent.onStunChangedEvent += OnStunChanged;
        }
    }

    void OnDisable()
    {
        if (isServer)
        {
            _attackComponent.onAttackEndedEvent -= OnAttackEnded;
            _attackComponent.onAttackStartedEvent -= OnAttackStarted;
            _enemyStunComponent.onStunChangedEvent -= OnStunChanged;
        }
    }

    protected virtual void Update()
    {
        if (!isServer) return;

        if (_isAttacking) return;

        if (_enemyStunComponent.IsStunned) return;

        _playerRecheckTimer += Time.deltaTime;
        if (_playerRecheckTimer >= _playerRecheckInterval)
        {
            _playerRecheckTimer = 0f;
            _currentTarget = _playerDistanceDetector.DetectClosestPlayer();

            if (_currentTarget != null)
            {
                _chaseBehaviour.SetTarget(_currentTarget);
                _chaseBehaviour.StartChasing();
            }
            else
            {
                _chaseBehaviour.StopChasing();
            }
        }
        if (_currentTarget != null)
        {
            _attackComponent.TryAttackIfInRange(_currentTarget);
        }
    }



    void OnAttackStarted()
    {
        _isAttacking = true;
        _playerRecheckTimer = 0f; // Reset the recheck timer to avoid chasing while attacking
        _chaseBehaviour.StopChasing(); // Stop chasing when attacking
    }
    void OnAttackEnded()
    {
        _isAttacking = false;
        _chaseBehaviour.StartChasing(); // Resume chasing after attack ends
    }

    void OnStunChanged(EnemyStunComponent.StunChangeInfo stunInfo)
    {
        _isStunned = stunInfo.isStunned;
        if (_isStunned)
        {
            _chaseBehaviour.StopChasing();
            _isAttacking = false; // Stop attacking when stunned
        }
        else
        {
            _chaseBehaviour.StartChasing();
        }
    }
}