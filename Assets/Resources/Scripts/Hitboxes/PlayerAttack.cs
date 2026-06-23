using UnityEngine;

[RequireComponent(typeof(Hitbox))]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float _stunDuration = 10f; // Duration of the stun effect in seconds
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
        // Check if the hit object has an EnemyStunComponent
        EnemyStunComponent stunComponent = hitObject.GetComponent<EnemyStunComponent>();
        if (stunComponent != null)
        {
            // Apply the stun effect
            stunComponent.Stun(_stunDuration);
        }
    }
}