using UnityEngine;
using Mirror;

[RequireComponent(typeof(ChaseBehaviour))]
[RequireComponent(typeof(AttackComponent))]
[RequireComponent(typeof(PlayerDistanceDetector))]
[RequireComponent(typeof(EnemyStunComponent))]
public class BasicEnemy : NetworkBehaviour
{
    [SerializeField] private float _attackRange = 2f; // Range within which the enemy can attack players
    [SerializeField] private float _playerRecheckInterval = 1f; // Interval to check for players
    private float _playerRecheckTimer = 0f;
    private ChaseBehaviour _chaseBehaviour;
    private AttackComponent _attackComponent;
    private PlayerDistanceDetector _playerDistanceDetector;
    private EnemyStunComponent _enemyStunComponent;
    private RaycastHit[] _raycastHitBuffer = new RaycastHit[10]; // Preallocate array for raycast hits
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

    void Update()
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
            TryAttackPlayer(_currentTarget);
        }
    }

    void TryAttackPlayer(GameObject player)
    {
        if (player == null) return;

        Vector3 directionToPlayer = player.transform.position - transform.position;
        directionToPlayer.y = 0; // Ignore vertical difference for the raycast
        directionToPlayer.Normalize();
        int hitCount = Physics.RaycastNonAlloc(transform.position, directionToPlayer, _raycastHitBuffer, _attackRange);

        for (int i = 0; i < hitCount; i++)
        {
            if (_raycastHitBuffer[i].collider.gameObject == player)
            {
                _attackComponent.TryAttack();
                break;
            }
        }
    }

    void OnAttackStarted()
    {
        _isAttacking = true;
        _playerRecheckTimer = 0f; // Reset the recheck timer to avoid chasing while attacking
    }
    void OnAttackEnded()
    {
        _isAttacking = false;
    }

    void OnStunChanged(EnemyStunComponent.StunChangeInfo stunInfo)
    {
        _isStunned = stunInfo.isStunned;
        if (_isStunned)
        {
            _chaseBehaviour.StopChasing();
            _isAttacking = false; // Stop attacking when stunned
        }
    }
}