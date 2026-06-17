using UnityEngine;
using Mirror;
using System;

[RequireComponent(typeof(Animator))]
public class AttackComponent : MonoBehaviour
{
    public Action onAttackStartedEvent;
    public Action onAttackEndedEvent;
    [SerializeField] private float _attackCooldown = 1f; // Time between attacks
    private float _currentAttackCooldown = 0f;
    private Animator _animator;

    void Awake()
    {
        _animator = GetComponent<Animator>();
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
            _currentAttackCooldown = _attackCooldown; // Reset cooldown
            onAttackStartedEvent?.Invoke(); // Notify that the attack has started
            _animator.SetTrigger("Attack"); // Trigger the attack animation
            return true; // Attack initiated
        }
        return false; // Attack not initiated due to cooldown
    }

    public void OnAttackAnimationEnd()
    {
        onAttackEndedEvent?.Invoke();
    }
}