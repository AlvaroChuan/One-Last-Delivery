using Adrenak.UniVoice.Samples;
using UnityEngine;

[RequireComponent(typeof(PlayerMovementComponent))]
[RequireComponent(typeof(SpectatorMovementComponent))]
[RequireComponent(typeof(PlayerVoiceDeathComponent))]
public class PlayerDeathComponent : PlayerComponent
{
    private PlayerMovementComponent _movementComponent;
    private SpectatorMovementComponent _spectatorComponent;
    private PlayerVoiceDeathComponent _deathComponent;

    void Awake()
    {
        _movementComponent = GetComponent<PlayerMovementComponent>();
        _spectatorComponent = GetComponent<SpectatorMovementComponent>();
        _deathComponent = GetComponent<PlayerVoiceDeathComponent>();
    }

    protected override void Start()
    {
        base.Start();
        _spectatorComponent.enabled = false; // Start with spectator movement disabled/
    }

    public void Die()
    {
        if (!isLocalPlayer) return;

        Debug.Log("Player has died. Switching to spectator mode.");

        _deathComponent.CmdNotifyDeathOnNetwork();

        _movementComponent.enabled = false;
        _spectatorComponent.enabled = true;
    }
}