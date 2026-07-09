using UnityEngine;
using Mirror;
using System;

[RequireComponent(typeof(Animator))]
public class EnemyAttackComponent : MonoBehaviour
{
    public event Action onAttackStartedEvent;
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    [SerializeField] private float _playerDetectionInterval = 0.05f; // Interval for checking player detection
    [SerializeField] private float _attackCooldown = 1f; // Time between attacks
    [SerializeField] private float _attackRange = 2f; // Range within which the enemy can attack players
    [SerializeField] private Hitbox _hitbox;
    private float _currentAttackCooldown = 0f;
    private NetworkAnimator _networkAnimator;
    private RaycastHit[] _raycastHitBuffer = new RaycastHit[10]; // Preallocate array for raycast hits
    private bool _isAttacking = false;
    public bool IsAttacking => _isAttacking;
    private float _playerDetectionTimer = 0f; // Timer for player detection checks

    void Awake()
    {
        _networkAnimator = GetComponent<NetworkAnimator>();

        if (_hitbox == null)
        {
            _hitbox = GetComponentInChildren<Hitbox>();
            if (_hitbox == null)
            {
                Debug.LogError("Hitbox component not found in children of AttackComponent.");
            }
        }
    }

    void Update()
    {
        if (_currentAttackCooldown > 0f)
        {
            _currentAttackCooldown -= Time.deltaTime;
        }
        if (_playerDetectionTimer > 0f)
        {
            _playerDetectionTimer -= Time.deltaTime;
        }
    }

    public void TryAttackIfInRange(GameObject target)
    {
        if (target == null || _playerDetectionTimer > 0f) return;

        _playerDetectionTimer = _playerDetectionInterval; // Reset the detection timer

        int hitCount = Physics.RaycastNonAlloc(transform.position, transform.forward, _raycastHitBuffer, _attackRange);

        for (int i = 0; i < hitCount; i++)
        {
            if (_raycastHitBuffer[i].collider.gameObject == target)
            {
                TryAttack();
                break;
            }
        }
    }

    public bool TryAttack()
    {
        if (_currentAttackCooldown <= 0f)
        {
            _currentAttackCooldown = _attackCooldown; // Reset cooldown
            _networkAnimator.SetTrigger("Attack"); // Trigger the attack animation
            _isAttacking = true;
            onAttackStartedEvent?.Invoke();
            return true; // Attack initiated
        }
        return false; // Attack not initiated due to cooldown
    }

    public void OnAttackAnimationEnd()
    {
        _isAttacking = false;
    }

    public void EnableHitbox()
    {
        if (_hitbox != null)
        {
            _hitbox.EnableHitbox();
        }
    }

    public void DisableHitbox()
    {
        if (_hitbox != null)
        {
            _hitbox.DisableHitbox();
        }
    }
    public void CancelAttack()
    {
        if (!_isAttacking) return;
        _isAttacking = false;
        if (_hitbox != null)
        {
            _hitbox.DisableHitbox();
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Draw a line to visualize the attack range
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * _attackRange);
    }
#endif
}