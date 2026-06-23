using Mirror;
using UnityEngine;

[RequireComponent(typeof(FieldOfViewDetector))]
[RequireComponent(typeof(NavMeshMovementComponent))]
[RequireComponent(typeof(PlayerChaseBehaviour))]
[RequireComponent(typeof(EnemyStunComponent))]
[RequireComponent(typeof(EnemyAttackComponent))]
public class Specter : NetworkBehaviour
{
    [SerializeField] float _fovCheckInterval = 0.05f;
    [SerializeField] float _playerDetectionRadius = 10f; // Radius within which to detect players
    private FieldOfViewDetector _fieldOfViewDetector;
    private NavMeshMovementComponent _movementComponent;
    private PlayerChaseBehaviour _playerChaseBehaviour;
    private EnemyStunComponent _enemyStunComponent;
    private EnemyAttackComponent _enemyAttackComponent;
    int _inFOVCount = 0;
    float _fovCheckTimer = 0f;
    bool _isInFOV = false;

    private void Awake()
    {
        _fieldOfViewDetector = GetComponent<FieldOfViewDetector>();
        _movementComponent = GetComponent<NavMeshMovementComponent>();
        _playerChaseBehaviour = GetComponent<PlayerChaseBehaviour>();
        _enemyStunComponent = GetComponent<EnemyStunComponent>();
        _enemyAttackComponent = GetComponent<EnemyAttackComponent>();

        _fovCheckTimer = Random.Range(0f, _fovCheckInterval); // Randomize the initial timer to avoid all Specters checking FOV at the same time
    }

    void OnEnable()
    {
        _enemyAttackComponent.onAttackEndedEvent += OnAttackEnded;
        _enemyAttackComponent.onAttackStartedEvent += OnAttackStarted;
        _enemyStunComponent.onStunChangedEvent += OnStunChanged;
    }

    void OnDisable()
    {
        _enemyAttackComponent.onAttackEndedEvent -= OnAttackEnded;
        _enemyAttackComponent.onAttackStartedEvent -= OnAttackStarted;
        _enemyStunComponent.onStunChangedEvent -= OnStunChanged;
    }
    void Update()
    {
        _fovCheckTimer += Time.deltaTime;
        if (_fovCheckTimer >= _fovCheckInterval)
        {
            _fovCheckTimer = 0f;
            CheckFOV();
        }

        if(!isServer) return;

        if (_enemyAttackComponent.IsAttacking) return;
        if (_enemyStunComponent.IsStunned) return;

        if (_inFOVCount > 0)
        {
            if (_movementComponent.Target != null)
            {
                _movementComponent.SetTarget(null); // Clear target when a player is in FOV
            }
            return;
        }
        else if (_inFOVCount <= 0)
        {
            if (_movementComponent.Target == null)
            {
                _playerChaseBehaviour.CheckForPlayer(_playerDetectionRadius); // Recheck for players after resuming movement
            }
            _playerChaseBehaviour.UpdateChase(Time.deltaTime, _playerDetectionRadius);

            if (_playerChaseBehaviour.IsChasing)
            {
                _enemyAttackComponent.TryAttackIfInRange(_playerChaseBehaviour.CurrentTarget);
            }
        }
    }

    void CheckFOV()
    {
        bool oldIsInFOV = _isInFOV;
        _isInFOV = _fieldOfViewDetector.IsInFOV(_playerDetectionRadius);
        if (_isInFOV && !oldIsInFOV)
        {
            CmdEnteredFOV();
        }
        else if (!_isInFOV && oldIsInFOV)
        {
            CmdExitedFOV();
        }
    }

    [Command(requiresAuthority = false)]
    void CmdEnteredFOV()
    {
        _inFOVCount++;
    }
    [Command(requiresAuthority = false)]
    void CmdExitedFOV()
    {
        _inFOVCount--;
    }

    void OnAttackStarted()
    {
        if (!isServer) return;

        _movementComponent.SetTarget(null); // Clear target when attack starts
    }

    void OnAttackEnded()
    {
        if (!isServer) return;

        _playerChaseBehaviour.CheckForPlayer(_playerDetectionRadius); // Recheck for players after attack
    }

    void OnStunChanged(EnemyStunComponent.StunChangeInfo stunInfo)
    {
        if (!isServer) return;

        if (stunInfo.isStunned)
        {
            _movementComponent.SetTarget(null); // Clear target when stunned
        }
        else
        {
            _playerChaseBehaviour.CheckForPlayer(_playerDetectionRadius); // Recheck for players after stun ends
        }
    }
}