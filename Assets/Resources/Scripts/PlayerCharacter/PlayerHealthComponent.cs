using System;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(PlayerDeathComponent))]
public class PlayerHealthComponent : PlayerComponent
{
    public struct HealthChangeInfo
    {
        public float oldHealth;
        public float newHealth;
        public float maxHealth;
    }

    public Action<HealthChangeInfo> onHealthChangedEvent;
    [SerializeField]
    float _maxHealth = 100f;
    public float MaxHealth => _maxHealth;
    float _currentHealth;
    public float CurrentHealth => _currentHealth;
    PlayerDeathComponent _playerDeathComponent;

    void Awake()
    {
        _playerDeathComponent = GetComponent<PlayerDeathComponent>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _currentHealth = _maxHealth;
    }

#if UNITY_EDITOR
    [SerializeField] private bool _die = false;
    protected override void OnValidate()
    {
        base.OnValidate();
        if (_die)
        {
            _die = false;
            RpcTakeDamage(_maxHealth);
        }
    }
#endif

    [ClientRpc]
    public void RpcTakeDamage(float damage)
    {
        if (!isLocalPlayer) return;

        if (_currentHealth <= 0)
            return;

        float oldHealth = _currentHealth;

        _currentHealth -= damage;

        DevLogger.Log($"Player took {damage} damage. Health: {_currentHealth}/{_maxHealth}");

        onHealthChangedEvent?.Invoke(new HealthChangeInfo
        {
            oldHealth = oldHealth,
            newHealth = _currentHealth,
            maxHealth = _maxHealth
        });

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Die();
        }
    }

    [ClientRpc]
    public void RpcHeal(float healAmount)
    {
        if (!isLocalPlayer) return;

        if (_currentHealth <= 0)
            return;

        float oldHealth = _currentHealth;
        _currentHealth = Mathf.Min(_currentHealth + healAmount, _maxHealth);

        onHealthChangedEvent?.Invoke(new HealthChangeInfo
        {
            oldHealth = oldHealth,
            newHealth = _currentHealth,
            maxHealth = _maxHealth
        });
    }

    void Die()
    {
        _playerDeathComponent.Die();
    }
}