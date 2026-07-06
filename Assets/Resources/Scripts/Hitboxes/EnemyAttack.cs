using Mirror;
using UnityEngine;

[RequireComponent(typeof(Hitbox))]
public class EnemyAttack : MonoBehaviour
{
    [SerializeField] private float _damage = 10f; // Damage dealt by the hitbox
    Hitbox _hitbox;

    void Awake()
    {
        _hitbox = GetComponent<Hitbox>();
    }

    void OnEnable()
    {
        _hitbox.onHit += HandleHit;
    }

    void OnDisable()
    {
        _hitbox.onHit -= HandleHit;
    }

    void HandleHit(GameObject hitObject)
    {
        if (!NetworkServer.active) return; // Ensure this runs only on the server

        DevLogger.Log($"EnemyAttack hit: {hitObject.name}");
        var healthComponent = hitObject.GetComponent<PlayerHealthComponent>();
        if (healthComponent != null)
        {
            healthComponent.ServerTakeDamage(_damage);
        }
    }
}