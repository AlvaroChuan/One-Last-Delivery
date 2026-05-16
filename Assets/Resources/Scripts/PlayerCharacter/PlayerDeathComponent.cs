using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovementComponent))]
[RequireComponent(typeof(SpectatorMovementComponent))]
public class PlayerDeathComponent : PlayerComponent
{
    private PlayerMovementComponent _movementComponent;
    private SpectatorMovementComponent _spectatorComponent;

    void Awake()
    {
        _movementComponent = GetComponent<PlayerMovementComponent>();
        _spectatorComponent = GetComponent<SpectatorMovementComponent>();
    }

    protected override void Start()
    {
        base.Start();
        _spectatorComponent.enabled = false; // Start with spectator movement disabled
    }

    public void Die()
    {
        if (!isLocalPlayer) return;

        Debug.Log("Player has died. Switching to spectator mode.");
        _movementComponent.enabled = false;
        _spectatorComponent.enabled = true;
    }
}