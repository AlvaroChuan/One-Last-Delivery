using UnityEngine;

[RequireComponent(typeof(Projectile))]
public class TaserProjectile : MonoBehaviour
{
    [SerializeField] private float _stunDuration = 2f; // Duration of the stun effect in seconds

    private Projectile _projectile;

    void Awake()
    {
        _projectile = GetComponent<Projectile>();
        _projectile.onProjectileHit += HandleProjectileHit;
    }

    void OnDestroy()
    {
        _projectile.onProjectileHit -= HandleProjectileHit;
    }

    private void HandleProjectileHit(GameObject hitObject)
    {
        // Check if the hit object has an EnemyStunComponent
        EnemyStunComponent stunComponent = hitObject.GetComponent<EnemyStunComponent>();
        if (stunComponent != null)
        {
            // Apply the stun effect
            stunComponent.Stun(_stunDuration);
        }
    }
}