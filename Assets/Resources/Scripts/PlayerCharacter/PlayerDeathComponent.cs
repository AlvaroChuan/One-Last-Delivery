using System;
using UnityEngine;

[RequireComponent(typeof(PlayerMovementComponent))]
[RequireComponent(typeof(SpectatorMovementComponent))]
public class PlayerDeathComponent : PlayerComponent
{
    public Action onPlayerDeathEvent;
    private PlayerMovementComponent _movementComponent;
    private SpectatorMovementComponent _spectatorComponent;

    void Awake()
    {
        _movementComponent = GetComponent<PlayerMovementComponent>();
        _spectatorComponent = GetComponent<SpectatorMovementComponent>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        _spectatorComponent.enabled = false; // Start with spectator movement disabled
    }

    public void Die()
    {
        if (!isLocalPlayer) return;

        DevLogger.Log("Player has died. Switching to spectator mode.");
        _movementComponent.enabled = false;
        _spectatorComponent.enabled = true;
        onPlayerDeathEvent?.Invoke();
    }
}