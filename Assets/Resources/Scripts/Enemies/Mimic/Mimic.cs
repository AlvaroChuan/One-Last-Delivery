using System.Collections;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerDistanceDetector))]
[RequireComponent(typeof(EnemyAttack))]
[RequireComponent(typeof(EnemyStunComponent))]
public class Mimic : NetworkBehaviour
{
    [System.Serializable]
    private struct ModelEntry
    {
        public GameObject packageModel;
        public GameObject monsterModel;
    }
    [Header("Settings")]
    [SerializeField] private float _attackCooldown = 5f;
    [SerializeField] private float _horizontalLungeForce = 10f;
    [SerializeField] private float _verticalLungeForce = 1f;
    [SerializeField] private float _transformDuration = 1f;
    [SerializeField] private float _playerCheckInterval = 1f;
    [SerializeField] private float _playerDetectionRadius = 10f;

    [Header("References")]
    [SerializeField] private ParticleSystem _transformationParticles;
    [SerializeField] private Collider _hitboxCollider;
    [SerializeField] private ModelEntry[] _modelEntries;

    [SyncVar(hook = nameof(OnIsTransformedChanged))]
    private bool _isTransformed = false;
    public bool IsTransformed => _isTransformed;
    private GameObject _packageModel;
    private GameObject _monsterModel;
    private PlayerDistanceDetector _playerDistanceDetector;
    private Rigidbody _rigidbody;
    private EnemyStunComponent _stunComponent;
    private GameObject _closestPlayer;
    private float _attackCooldownTimer = 0f;
    private float _playerCheckTimer = 0f;
    private Coroutine _resetTransformationCoroutine;

    void Awake()
    {
        int randomIndex = Random.Range(0, _modelEntries.Length);
        _packageModel = _modelEntries[randomIndex].packageModel;
        _monsterModel = _modelEntries[randomIndex].monsterModel;

        for (int i = 0; i < _modelEntries.Length; i++)
        {
            if (i != randomIndex)
            {
                _modelEntries[i].packageModel.SetActive(false);
                _modelEntries[i].monsterModel.SetActive(false);
            }
        }

        _playerDistanceDetector = GetComponent<PlayerDistanceDetector>();
        _rigidbody = GetComponent<Rigidbody>();
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

        // Reset the hitbox collider to ensure it detects the player during the lunge
        _hitboxCollider.enabled = false;
        _hitboxCollider.enabled = true;

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
                _hitboxCollider.enabled = false; // Disable the hitbox collider when stunned
            }
        }
    }
}