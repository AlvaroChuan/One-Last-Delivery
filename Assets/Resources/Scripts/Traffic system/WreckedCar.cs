using System;
using Mirror;
using UnityEngine;

public class WreckedCar : NetworkBehaviour
{
    public static Action OnCarExploded;
    [SerializeField] float _explosionDelay = 5f;
    private float _explosionTimer = 0f;

    public override void OnStartServer()
    {
        _explosionTimer = _explosionDelay;
    }

    private void OnEnable()
    {
        WreckedCarFader.OnFadeCompleted += OnFadeCompleted;
    }

    private void OnDisable()
    {
        WreckedCarFader.OnFadeCompleted -= OnFadeCompleted;
    }

    void Update()
    {
        if(!isServer) return;

        _explosionTimer -= Time.deltaTime;
        if(_explosionTimer <= 0f) OnCarExploded?.Invoke();
    }

    private void OnFadeCompleted()
    {
        NetworkServer.Destroy(gameObject);
    }
}
