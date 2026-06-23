using System;
using System.Collections;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : NetworkBehaviour
{
    public Action<GameObject> onProjectileHit; // Event to notify when the projectile hits something
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _lifetime = -1f;
    [SerializeField] private float _range = -1f;
    [SerializeField] bool _gravityEnabled = false;
    [SerializeField] private ParticleSystem _impactParticlesPrefab;
    Rigidbody _rigidbody;
    Vector3 _startPosition;

    public override void OnStartServer()
    {
        if (_lifetime > 0f)
        {
            StartCoroutine(DestroyAfterLifetime());
        }
        _startPosition = transform.position;
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = _gravityEnabled;
        _rigidbody.linearVelocity = transform.forward * _speed;
    }

    void Update()
    {
        if (!isServer) return;

        if (_range > 0f && Vector3.Distance(_startPosition, transform.position) >= _range)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        if (!isServer) return;

        DevLogger.Log($"Projectile hit: {collider.gameObject.name}");

        onProjectileHit?.Invoke(collider.gameObject);

        NetworkServer.Spawn(Instantiate(_impactParticlesPrefab, transform.position, Quaternion.identity).gameObject);

        NetworkServer.Destroy(gameObject);
    }

    IEnumerator DestroyAfterLifetime()
    {
        yield return new WaitForSeconds(_lifetime);
        NetworkServer.Destroy(gameObject);
    }
}