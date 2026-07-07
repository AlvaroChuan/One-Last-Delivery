using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(ParticleSystem))]
public class SpawnBloodParticles : NetworkBehaviour
{
    [SerializeField] private GameObject _puddlePrefab;
    [SerializeField] private float _surfaceOffset = 0.01f;

    private ParticleSystem _mainParticleSystem;
    private List<ParticleCollisionEvent> _collisionEvents = new List<ParticleCollisionEvent>();

    private void Start()
    {
        _mainParticleSystem = GetComponent<ParticleSystem>();
    }

    private void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = _mainParticleSystem.GetCollisionEvents(other, _collisionEvents);

        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 collisionPoint = _collisionEvents[i].intersection;
            Vector3 surfaceNormal = _collisionEvents[i].normal;

            NetworkIdentity hitIdentity = other.GetComponentInParent<NetworkIdentity>();

            if (isServer)
            {
                RpcSpawnParticleLocal(collisionPoint, surfaceNormal, hitIdentity);
            }
            else if (isClient)
            {
                CmdRequestParticleSpawn(collisionPoint, surfaceNormal, hitIdentity);
            }
        }
    }

    [Command]
    private void CmdRequestParticleSpawn(Vector3 position, Vector3 normal, NetworkIdentity targetNetIdentity)
    {
        RpcSpawnParticleLocal(position, normal, targetNetIdentity);
    }

    [ClientRpc]
    private void RpcSpawnParticleLocal(Vector3 position, Vector3 normal, NetworkIdentity targetNetIdentity)
    {
        if (isServer && isServerOnly) return;

        Vector3 spawnPosition = position + (normal * _surfaceOffset);
        Quaternion targetRotation = Quaternion.LookRotation(normal);
        targetRotation *= Quaternion.Euler(0, 0, 0);

        GameObject spawnedPuddle = Instantiate(_puddlePrefab, spawnPosition, targetRotation);

        if (targetNetIdentity != null)
        {
            spawnedPuddle.transform.SetParent(targetNetIdentity.transform, true);
        }
    }
}