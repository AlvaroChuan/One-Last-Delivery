using System.Collections;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerDistanceDetector))]
[RequireComponent(typeof(EnemyHitbox))]
[RequireComponent(typeof(EnemyStunComponent))]
public class Mimic : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _attackCooldown = 5f;
    [SerializeField] private float _horizontalLungeForce = 10f;
    [SerializeField] private float _verticalLungeForce = 1f;
    [SerializeField] private float _transformDuration = 1f;
    [SerializeField] private float _playerCheckInterval = 1f;
    [SerializeField] private float _playerDetectionRadius = 10f;

    [Header("References")]
    [SerializeField] private GameObject _packageModel;
    [SerializeField] private GameObject _monsterModel;
    [SerializeField] private ParticleSystem _transformationParticles;
    [SerializeField] private Collider _hitboxCollider;

    [SyncVar(hook = nameof(OnIsTransformedChanged))]
    private bool _isTransformed = false;
    public bool IsTransformed => _isTransformed;

    private PlayerDistanceDetector _playerDistanceDetector;
    private Rigidbody _rigidbody;
    private EnemyHitbox _hitbox;
    private EnemyStunComponent _stunComponent;
    private GameObject _closestPlayer;
    private float _attackCooldownTimer = 0f;
    private float _playerCheckTimer = 0f;
    private Coroutine _resetTransformationCoroutine;

    void Awake()
    {
        _playerDistanceDetector = GetComponent<PlayerDistanceDetector>();
        _rigidbody = GetComponent<Rigidbody>();
        _hitbox = GetComponent<EnemyHitbox>();
        _stunComponent = GetComponent<EnemyStunComponent>();

        _playerCheckTimer = Random.Range(0f, _playerCheckInterval); // Randomize the initial timer to avoid all Mimics checking for players at the same time
    }

    void OnEnable()
    {
        _stunComponent.onStunChangedEvent += OnStunChanged;
    }

    void OnDisable()
    {
        _stunComponent.onStunChangedEvent -= OnStunChanged;
    }

    void Update()
    {
        if (!isServer) return;

        if (_stunComponent.IsStunned) return;

        if(_isTransformed && _closestPlayer != null)
        {
            transform.LookAt(new Vector3(_closestPlayer.transform.position.x, transform.position.y, _closestPlayer.transform.position.z));
        }

        if (_attackCooldownTimer > 0f) _attackCooldownTimer -= Time.deltaTime;
        if (_playerCheckTimer > 0f) _playerCheckTimer -= Time.deltaTime;

        if (_attackCooldownTimer > 0f || _playerCheckTimer > 0f) return;

        _playerCheckTimer = _playerCheckInterval;

        if ((_closestPlayer = _playerDistanceDetector.DetectClosestPlayer(_playerDetectionRadius)) != null)
        {
            if (!_isTransformed)
            {
                TransformIntoMonster();
                _hitbox.enabled = true; // Enable the hitbox when transformed
                _hitboxCollider.enabled = true; // Enable the hitbox collider when transformed
            }
            LungeAtPlayer();
        }
    }

    [Server]
    public void TransformIntoMonster()
    {
        _isTransformed = true;
    }

    private void OnIsTransformedChanged(bool oldValue, bool newValue)
    {
        _packageModel.SetActive(!newValue);
        _monsterModel.SetActive(newValue);
        _transformationParticles.Play();
    }

    [Server]
    void LungeAtPlayer()
    {
        if (_closestPlayer == null) return;

        Vector3 direction = _closestPlayer.transform.position - transform.position;
        direction.y = 0;
        direction.Normalize();
        _rigidbody.AddForce(direction * _horizontalLungeForce + Vector3.up * _verticalLungeForce, ForceMode.Impulse);

        _attackCooldownTimer = _attackCooldown;

        if (_resetTransformationCoroutine != null)
        {
            StopCoroutine(_resetTransformationCoroutine);
        }
        _resetTransformationCoroutine = StartCoroutine(ResetTransformation());
    }

    [Server]
    IEnumerator ResetTransformation()
    {
        yield return new WaitForSeconds(_transformDuration);
        _isTransformed = false;
        _hitbox.enabled = false; // Disable the hitbox when not transformed
        _hitboxCollider.enabled = false; // Disable the hitbox collider when not transformed
    }

    private void OnStunChanged(EnemyStunComponent.StunChangeInfo info)
    {
        if (!isServer) return;

        if (info.isStunned)
        {
            if (_isTransformed)
            {
                if (_resetTransformationCoroutine != null)
                {
                    StopCoroutine(_resetTransformationCoroutine);
                    _resetTransformationCoroutine = null;
                }
                _isTransformed = false;
                _hitbox.enabled = false; // Disable the hitbox when stunned
                _hitboxCollider.enabled = false; // Disable the hitbox collider when stunned
            }
        }
    }
}