using UnityEngine;
using Mirror;

[RequireComponent(typeof(ChaseBehaviour))]
[RequireComponent(typeof(AttackComponent))]
[RequireComponent(typeof(PlayerDistanceDetector))]
public class BasicEnemy : NetworkBehaviour
{
    [SerializeField] private float _detectionRange = 10f; // Range within which the enemy can detect players
    [SerializeField] private float _attackRange = 2f; // Range within which the enemy can attack players
    [SerializeField] private float _playerRecheckInterval = 1f; // Interval to check for players
    private float _playerRecheckTimer = 0f;
    private ChaseBehaviour _chaseBehaviour;
    private AttackComponent _attackComponent;
    private PlayerDistanceDetector _playerDistanceDetector;
    private RaycastHit[] _raycastHitBuffer = new RaycastHit[10]; // Preallocate array for raycast hits
    private bool _isAttacking = false;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _chaseBehaviour = GetComponent<ChaseBehaviour>();
        _attackComponent = GetComponent<AttackComponent>();
        _playerDistanceDetector = GetComponent<PlayerDistanceDetector>();
        _playerRecheckTimer = Random.Range(0f, _playerRecheckInterval); // Randomize initial timer to avoid synchronized checks
    }

    void OnEnable()
    {
        if (isServer)
        {
            _attackComponent.onAttackEndedEvent += OnAttackEnded;
            _attackComponent.onAttackStartedEvent += OnAttackStarted;
        }
    }

    void OnDisable()
    {
        if (isServer)
        {
            _attackComponent.onAttackEndedEvent -= OnAttackEnded;
            _attackComponent.onAttackStartedEvent -= OnAttackStarted;
        }
    }

    void Update()
    {
        if (!isServer) return;

        if (_isAttacking) return;

        _playerRecheckTimer += Time.deltaTime;
        if (_playerRecheckTimer >= _playerRecheckInterval)
        {
            _playerRecheckTimer = 0f;
            GameObject closestPlayer = _playerDistanceDetector.DetectClosestPlayer();

            if (closestPlayer != null)
            {
                _chaseBehaviour.SetTarget(closestPlayer);
                _chaseBehaviour.StartChasing();
                TryAttackPlayer(closestPlayer);
            }
            else
            {
                _chaseBehaviour.StopChasing();
            }
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
}