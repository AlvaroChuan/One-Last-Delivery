using UnityEngine;

public class ProjectileSound : MonoBehaviour
{
    [SerializeField] private AudioEvent _projectileHitAudioEvent;

    void OnEnable()
    {
        GetComponent<Projectile>().onProjectileHitRemote += HandleProjectileHit;
    }

    void OnDisable()
    {
        GetComponent<Projectile>().onProjectileHitRemote -= HandleProjectileHit;
    }

    private void HandleProjectileHit(GameObject hitObject)
    {
        _projectileHitAudioEvent.Play(gameObject.transform.position);
    }
}