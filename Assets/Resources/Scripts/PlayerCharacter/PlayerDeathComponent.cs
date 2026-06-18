using System;
using NUnit.Framework;
using UnityEngine;

[RequireComponent(typeof(PlayerMovementComponent))]
[RequireComponent(typeof(SpectatorMovementComponent))]
public class PlayerDeathComponent : PlayerComponent
{
    public Action onPlayerDeathEvent;
    private PlayerMovementComponent _movementComponent;
    private SpectatorMovementComponent _spectatorComponent;
    public bool IsDead { get; private set; } = false;

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

        if (IsDead) return; // Prevent multiple death triggers

        DevLogger.Log("Player has died. Switching to spectator mode.");
        IsDead = true;
        _movementComponent.enabled = false;
        _spectatorComponent.enabled = true;
        onPlayerDeathEvent?.Invoke();
    }
}