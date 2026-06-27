using System;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PackageHealthComponent : NetworkBehaviour
{
    public struct HealthChangeInfo
    {
        public float oldHealth;
        public float newHealth;
    }
    public Action<HealthChangeInfo> onHealthChangedEvent;
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private LayerMask _damageLayerMask; // Layers that can cause damage to the package
    [SerializeField] private float _minForceForDamage = 5f;
    [SerializeField] private float _damagePerUnitForce = 1f;
    [SerializeField] private float _currentHealth;
    [SerializeField] private float _damageCooldown = 0.5f; // Time in seconds before the package can take damage again
    private Rigidbody _rigidbody;

    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _currentHealth;

    float _timeSinceLastDamage = 0f;

    private bool _canTakeDamage = true;
    public bool CanTakeDamage
    {
        get => _canTakeDamage;
        set => _canTakeDamage = value;
    }

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if(!isServer)
        {
            enabled = false;
        }
        _currentHealth = _maxHealth;
    }

    void Update()
    {
        if (!isServer) return;

        if (_timeSinceLastDamage < _damageCooldown)
        {
            _timeSinceLastDamage += Time.deltaTime;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if(_rigidbody.isKinematic) return;
        if (((1 << collision.gameObject.layer) & _damageLayerMask) == 0) return; // Check if the collided object's layer is in the damage layer mask

        float otherMass = (collision.rigidbody != null) ? collision.rigidbody.mass : _rigidbody.mass;

        float myMass = _rigidbody.mass;

        float effectiveMass = (otherMass * myMass) / (otherMass + myMass);

        float relativeVelocity = collision.relativeVelocity.magnitude;

        float force = 0.5f * effectiveMass * relativeVelocity * relativeVelocity;

        if (force < _minForceForDamage) return;

        float damage = (force - _minForceForDamage) * _damagePerUnitForce;

        if (isServer)
        {
            ServerTakeDamage(damage);
        }
        else if (isOwned)
        {
            CmdTakeDamage(damage);
        }
    }

    [Server]
    public void ServerTakeDamage(float damage)
    {
        DevLogger.Log($"Package trying to take damage: {damage}. Current health: {_currentHealth}. Time since last damage: {_timeSinceLastDamage}/{_damageCooldown}. Can take damage: {_canTakeDamage}");
        if (_timeSinceLastDamage < _damageCooldown) return; // Ignore damage if within cooldown period
        if (!_canTakeDamage) return; // Ignore damage if the package cannot take damage

        _currentHealth -= damage;
        if (_currentHealth <= 0f)
        {
            _currentHealth = 0f;

            NetworkServer.Destroy(gameObject);
        }
        else
        {
            RpcUpdateHealth(_currentHealth);
        }
    }
    [Command]
    public void CmdTakeDamage(float damage)
    {
        ServerTakeDamage(damage);
    }

    [ClientRpc]
    public void RpcUpdateHealth(float newHealth)
    {
        float oldHealth = _currentHealth;
        _currentHealth = newHealth;

        onHealthChangedEvent?.Invoke(new HealthChangeInfo
        {
            oldHealth = oldHealth,
            newHealth = newHealth
        });
    }
}
