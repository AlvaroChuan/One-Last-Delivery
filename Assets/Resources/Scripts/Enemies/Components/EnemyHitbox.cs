using UnityEngine;
using Mirror;

public class EnemyHitbox : MonoBehaviour
{
    [SerializeField] private int _damage = 10; // Damage dealt by the hitbox

    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active) return; // Ensure this logic only runs on the server

        var healthComponent = other.GetComponent<PlayerHealthComponent>();
        if (healthComponent != null)
        {
            healthComponent.RpcTakeDamage(_damage);
        }
    }
}