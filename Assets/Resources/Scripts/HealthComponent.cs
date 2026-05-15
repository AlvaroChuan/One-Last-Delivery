using Mirror;
using UnityEngine;

public abstract class HealthComponent : NetworkBehaviour
{
    [SerializeField]
    int _maxHealth = 100;
    [SyncVar, SerializeField]
    int _currentHealth;

    void Start()
    {
        _currentHealth = _maxHealth;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
         {
             CmdTakeDamage(100);
         }
    }

    [Command]
    public void CmdTakeDamage(int damage)
    {
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
    protected abstract void Die();
}