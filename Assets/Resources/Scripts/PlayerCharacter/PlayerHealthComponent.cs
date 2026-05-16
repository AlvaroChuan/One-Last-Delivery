using Mirror;
using Mirror.Examples.Basic;
using UnityEngine;

[RequireComponent(typeof(PlayerDeathComponent))]
public class PlayerHealthComponent : PlayerComponent
{
    [SerializeField]
    float _maxHealth = 100f;
    float _currentHealth;
    PlayerDeathComponent _controller;

    void Awake()
    {
        _controller = GetComponent<PlayerDeathComponent>();
    }


    protected override void Start()
    {
        base.Start();
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
        Debug.Log($"RpcTakeDamage called with damage: {damage}");
        if (!isLocalPlayer) return;

        if (_currentHealth <= 0)
            return;

        Debug.Log($"Taking {damage} damage");
        _currentHealth -= damage;
        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Die();
        }
    }
    void Die()
    {
        _controller.Die();
    }
}