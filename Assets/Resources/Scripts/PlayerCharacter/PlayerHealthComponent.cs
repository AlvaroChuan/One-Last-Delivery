using System;
using Mirror;
using UnityEngine;

public class PlayerHealthComponent : PlayerComponent
{
    public struct HealthChangeInfo
    {
        public float oldHealth;
        public float newHealth;
        public float maxHealth;
    }

    public Action<HealthChangeInfo> onHealthChanged;
    [SerializeField] float _maxHealth = 100f;
    [SerializeField] private GameObject _bloodVFX;
    public float MaxHealth => _maxHealth;
    [SyncVar(hook = nameof(OnCurrentHealthChanged))] float _currentHealth;
    public float CurrentHealth => _currentHealth;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isServer) _currentHealth = _maxHealth;
    }

#if UNITY_EDITOR
    [SerializeField] private bool _die = false;
    protected override void OnValidate()
    {
        base.OnValidate();
        if (_die)
        {
            _die = false;
            ServerTakeDamage(_maxHealth);
        }
    }
#endif

    [Server]
    public void ServerTakeDamage(float damage)
    {
        if (_currentHealth <= 0) return;
        _currentHealth -= damage;
        if (_currentHealth <= 0) HandleDeath();
    }

    [Command(requiresAuthority = false)]
    public void CmdTakeDamage(float damage)
    {
        ServerTakeDamage(damage);
    }

    [Server]
    private void HandleDeath()
    {
        if (_bloodVFX != null)
        {
            GameObject vfx = Instantiate(_bloodVFX, transform.position, Quaternion.identity);
            NetworkServer.Spawn(vfx);
        }
    }

    void OnCurrentHealthChanged(float oldHealth, float newHealth)
    {
        HealthChangeInfo info = new HealthChangeInfo
        {
            oldHealth = oldHealth,
            newHealth = newHealth,
            maxHealth = _maxHealth
        };

        onHealthChanged?.Invoke(info);
    }
}