using UnityEngine;
using Mirror;
using System;

[RequireComponent(typeof(Animator))]
public class AttackComponent : MonoBehaviour
{
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    public Action onAttackStartedEvent;
    public Action onAttackEndedEvent;
    [SerializeField] private float _attackCooldown = 1f; // Time between attacks
    private float _currentAttackCooldown = 0f;
    private Animator _animator;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError("Animator component is missing on AttackComponent.");
        }
    }

    void Update()
    {
        if (_currentAttackCooldown > 0f)
        {
            _currentAttackCooldown -= Time.deltaTime;
        }
    }

    public bool TryAttack()
    {
        if (_currentAttackCooldown <= 0f)
        {
            DevLogger.Log("Attack initiated.");
            _currentAttackCooldown = _attackCooldown; // Reset cooldown
            onAttackStartedEvent?.Invoke(); // Notify that the attack has started
            _animator.SetTrigger(AttackHash); // Trigger the attack animation
            return true; // Attack initiated
        }
        return false; // Attack not initiated due to cooldown
    }

    public void OnAttackAnimationEnd()
    {
        DevLogger.Log("Attack animation ended.");
        onAttackEndedEvent?.Invoke();
    }
}