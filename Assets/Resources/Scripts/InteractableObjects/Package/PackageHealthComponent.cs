using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PackageHealthComponent : NetworkBehaviour
{
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _minImpulseForDamage = 1f;
    [SerializeField] private float _damagePerUnitImpulse = 10f;
    private float _currentHealth;
    private Rigidbody _rigidbody;

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

        Debug.Log($"Package collided with {collision.gameObject.name} at impulse {collision.impulse.magnitude}.");

        float impulse = collision.impulse.magnitude;

        if (impulse < _minImpulseForDamage) return;

        float damage = (impulse - _minImpulseForDamage) * _damagePerUnitImpulse;
        TakeDamage(damage);
    }

    [Server]
    public void TakeDamage(float damage)
    {
        Debug.Log($"Package took {damage} damage. Going from {_currentHealth} to {_currentHealth - damage} health.");
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
        Debug.Log("Package destroyed.");
        // Here you can add code to play destruction effects on clients
    }
}
