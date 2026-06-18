using System;
using Mirror;
using UnityEngine;

public class WreckedCar : NetworkBehaviour
{
    public Action onDestroyedEvent;
    [SerializeField] float _explosionDelay = 5f;
    float _explosionTimer = 0f;

    public override void OnStartServer()
    {
        _explosionTimer = _explosionDelay;
    }

    void Update()
    {
        if(!isServer) return;

        _explosionTimer -= Time.deltaTime;
        if(_explosionTimer <= 0f)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        onDestroyedEvent?.Invoke();
    }
}
