using Mirror;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.Cinemachine;

[RequireComponent(typeof(FieldOfViewDetector))]
[RequireComponent(typeof(NavMeshMovementComponent))]
[RequireComponent(typeof(PlayerChaseBehaviour))]
[RequireComponent(typeof(EnemyStunComponent))]
[RequireComponent(typeof(EnemyAttackComponent))]
public class Specter : NetworkBehaviour
{
    [SerializeField] float _fovCheckInterval = 0.05f;
    [SerializeField] float _playerDetectionRadius = 10f;

    [SerializeField] private Animator _animator;

    [SerializeField] private Transform _handGrabPoint;
    [SerializeField] private Transform _specterFace;
    [SerializeField] private float _jumpscareDuration = 2.5f;
    [SerializeField] private float _jumpscareLethalDamage = 999f;

    private FieldOfViewDetector _fieldOfViewDetector;
    private NavMeshMovementComponent _movementComponent;
    private PlayerChaseBehaviour _playerChaseBehaviour;
    private EnemyStunComponent _enemyStunComponent;
    private EnemyAttackComponent _enemyAttackComponent;
    private NavMeshAgent _navMeshAgent;

    [SyncVar(hook = nameof(OnCurrentTargetChanged))]
    private GameObject _currentTarget;

    [SyncVar]
    private bool _isStoppedByTarget = false;

    float _fovCheckTimer = 0f;
    bool _localIsInFOV = false;

    private bool _isExecutingJumpscare = false;
    private bool _isCurrentlyStunned = false;

    private void Awake()
    {
        _fieldOfViewDetector = GetComponent<FieldOfViewDetector>();
        _movementComponent = GetComponent<NavMeshMovementComponent>();
        _playerChaseBehaviour = GetComponent<PlayerChaseBehaviour>();
        _enemyStunComponent = GetComponent<EnemyStunComponent>();
        _enemyAttackComponent = GetComponent<EnemyAttackComponent>();
        _navMeshAgent = GetComponent<NavMeshAgent>();

        _fovCheckTimer = Random.Range(0f, _fovCheckInterval);
    }

    void OnEnable()
    {
        _enemyAttackComponent.onAttackStartedEvent += OnAttackStarted;
        _enemyStunComponent.onStunChangedEvent += OnStunChanged;
    }

    void OnDisable()
    {
        _enemyAttackComponent.onAttackStartedEvent -= OnAttackStarted;
        _enemyStunComponent.onStunChangedEvent -= OnStunChanged;
    }

    void Update()
    {
        if (isClient)
        {
            _fovCheckTimer += Time.deltaTime;
            if (_fovCheckTimer >= _fovCheckInterval)
            {
                _fovCheckTimer = 0f;
                CheckFOV();
            }
        }

        if (!isServer) return;

        UpdateAnimations();

        if (_enemyAttackComponent.IsAttacking || _isExecutingJumpscare) return;
        if (_isCurrentlyStunned) return;

        if (_playerChaseBehaviour.CurrentTarget != _currentTarget)
        {
            _currentTarget = _playerChaseBehaviour.CurrentTarget;
            _isStoppedByTarget = false;
        }

        if (_isStoppedByTarget)
        {
            if (_movementComponent.Target != null)
            {
                _movementComponent.SetTarget(null);
            }
            return;
        }
        else
        {
            if (_movementComponent.Target == null)
            {
                _playerChaseBehaviour.CheckForPlayer(_playerDetectionRadius);
            }
            _playerChaseBehaviour.UpdateChase(Time.deltaTime, _playerDetectionRadius);

            if (_playerChaseBehaviour.IsChasing)
            {
                _enemyAttackComponent.TryAttackIfInRange(_playerChaseBehaviour.CurrentTarget);
            }
        }
    }

    [Client]
    void CheckFOV()
    {
        if (_currentTarget == null || NetworkClient.localPlayer == null || _currentTarget != NetworkClient.localPlayer.gameObject)
        {
            if (_localIsInFOV)
            {
                _localIsInFOV = false;
                CmdSetFOV(false);
            }
            return;
        }

        bool oldIsInFOV = _localIsInFOV;
        _localIsInFOV = _fieldOfViewDetector.IsInFOV(_playerDetectionRadius);

        if (_localIsInFOV && !oldIsInFOV)
        {
            CmdSetFOV(true);
        }
        else if (!_localIsInFOV && oldIsInFOV)
        {
            CmdSetFOV(false);
        }
    }

    [Command(requiresAuthority = false)]
    void CmdSetFOV(bool inFOV)
    {
        _isStoppedByTarget = inFOV;
    }

    void OnCurrentTargetChanged(GameObject oldTarget, GameObject newTarget)
    {
        if (isClient) _localIsInFOV = false;
    }

    void UpdateAnimations()
    {
        if (_animator == null || _navMeshAgent == null) return;

        bool isMoving = _navMeshAgent.velocity.sqrMagnitude > 0.01f;
        bool isChasing = _playerChaseBehaviour.IsChasing;

        _animator.SetBool("IsWalking", isMoving && !isChasing);
        _animator.SetBool("IsRunning", isMoving && isChasing);
    }

    void OnStunChanged(EnemyStunComponent.StunChangeInfo stunInfo)
    {
        if (!isServer) return;

        if (stunInfo.isStunned)
        {
            if (_enemyAttackComponent.IsAttacking || _isExecutingJumpscare) return;

            _isCurrentlyStunned = true;
            _movementComponent.SetTarget(null);
            if (_animator != null) _animator.SetBool("IsStunned", true);
        }
        else
        {
            _isCurrentlyStunned = false;
            if (_animator != null) _animator.SetBool("IsStunned", false);
        }
    }

    void OnAttackStarted()
    {
        if (!isServer) return;

        _isExecutingJumpscare = true;
        _movementComponent.SetTarget(null);

        if (_animator != null) _animator.SetTrigger("Attack");

        GameObject targetPlayer = _playerChaseBehaviour.CurrentTarget;
        if (targetPlayer != null)
        {
            NetworkIdentity targetIdentity = targetPlayer.GetComponent<NetworkIdentity>();
            if (targetIdentity != null)
            {
                TargetExecuteJumpscare(targetIdentity.connectionToClient, targetPlayer);
                StartCoroutine(ServerKillRoutine(targetPlayer));
            }
        }
        else
        {
            _isExecutingJumpscare = false;
        }
    }

    [Server]
    private IEnumerator ServerKillRoutine(GameObject targetPlayer)
    {
        yield return new WaitForSeconds(_jumpscareDuration);

        if (targetPlayer != null)
        {
            var health = targetPlayer.GetComponent<PlayerHealthComponent>();
            if (health != null) health.ServerTakeDamage(_jumpscareLethalDamage);
        }

        _isExecutingJumpscare = false;
    }

    [TargetRpc]
    private void TargetExecuteJumpscare(NetworkConnectionToClient conn, GameObject victim)
    {
        if (victim != null)
        {
            StartCoroutine(ClientJumpscareRoutine(victim));
        }
    }

    private IEnumerator ClientJumpscareRoutine(GameObject victim)
    {
        Rigidbody rb = victim.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        PlayerLookComponent lookComp = victim.GetComponent<PlayerLookComponent>();
        Transform playerEyes = lookComp != null ? lookComp.Eyes : victim.transform;

        CinemachineBrain cineBrain = null;
        float originalFov = 100f;

        if (Camera.main != null)
        {
            cineBrain = Camera.main.GetComponent<CinemachineBrain>();
            if (cineBrain != null) cineBrain.enabled = false;
            originalFov = Camera.main.fieldOfView;
        }

        float elapsed = 0f;
        Vector3 startPos = victim.transform.position;

        while (elapsed < _jumpscareDuration)
        {
            float t = Mathf.Clamp01(elapsed / 0.15f);

            if (_handGrabPoint != null)
            {
                victim.transform.position = Vector3.Lerp(startPos, _handGrabPoint.position, t);
            }

            yield return new WaitForEndOfFrame();

            if (Camera.main != null)
            {
                Camera.main.transform.position = playerEyes.position;
                Camera.main.fieldOfView = Mathf.Lerp(originalFov, 70f, t);

                if (_specterFace != null)
                {
                    Vector3 lookDir = (_specterFace.position - Camera.main.transform.position).normalized;
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        Camera.main.transform.rotation = Quaternion.LookRotation(lookDir);
                    }
                }
            }
            elapsed += Time.deltaTime;
        }

        if (rb != null) rb.isKinematic = false;

        if (Camera.main != null)
        {
            Camera.main.fieldOfView = originalFov;
        }

        if (cineBrain != null) cineBrain.enabled = true;
    }
}