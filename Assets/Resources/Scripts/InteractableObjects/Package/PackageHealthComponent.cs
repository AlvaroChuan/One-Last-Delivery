using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PackageHealthComponent : NetworkBehaviour
{
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _minForceForDamage = 5f;
    [SerializeField] private float _damagePerUnitForce = 1f;
    private float _currentHealth;
    private Rigidbody _rigidbody;

    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _currentHealth;

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

    void OnCollisionEnter(Collision collision)
    {
        if(!isServer || _rigidbody.isKinematic) return;

        float otherMass = (collision.rigidbody != null) ? collision.rigidbody.mass : _rigidbody.mass;

        float myMass = _rigidbody.mass;

        float effectiveMass = (otherMass * myMass) / (otherMass + myMass);

        float relativeVelocity = collision.relativeVelocity.magnitude;

        float force = 0.5f * effectiveMass * relativeVelocity * relativeVelocity;

        if (force < _minForceForDamage) return;

        float damage = (force - _minForceForDamage) * _damagePerUnitForce;

        TakeDamage(damage);
    }

    [Server]
    public void TakeDamage(float damage)
    {
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

    [ClientRpc]
    public void RpcUpdateHealth(float newHealth)
    {
        _currentHealth = newHealth;

        // Here you can add code to update health UI or play damage effects on clients
    }

    void OnDestroy()
    {
        // Here you can add code to play destruction effects on clients
    }
}
