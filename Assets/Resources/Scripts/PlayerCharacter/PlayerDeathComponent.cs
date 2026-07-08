using System;
using Mirror;
using Mirror.Examples.Basic;
using UnityEngine;
[RequireComponent(typeof(PlayerMovementComponent))]
[RequireComponent(typeof(PlayerLookComponent))]

[RequireComponent(typeof(PlayerSpectateComponent))]
[RequireComponent(typeof(PlayerInventoryComponent))]
[RequireComponent(typeof(PlayerHealthComponent))]
public class PlayerDeathComponent : PlayerComponent
{
    PlayerHealthComponent _playerHealthComponent;
    public Action onPlayerDeathEvent;
    public bool IsDead { get; private set; } = false;

    void Awake()
    {
        _playerHealthComponent = GetComponent<PlayerHealthComponent>();
        _playerHealthComponent.onHealthChanged += HandleHealthChanged;
    }

    void OnDestroy()
    {
        _playerHealthComponent.onHealthChanged -= HandleHealthChanged;
    }

    void HandleHealthChanged(PlayerHealthComponent.HealthChangeInfo info)
    {
        if (info.newHealth <= 0f && !IsDead)
        {
            Die();
        }
    }

    public override void OnStartClient()
    {
        GetComponent<PlayerSpectateComponent>().enabled = false; // Disable spectate component for the local player at the start
    }

    public void Die()
    {
        if (!isLocalPlayer) return;

        if (IsDead) return; // Prevent multiple death triggers

        DevLogger.Log("Player has died. Switching to spectator mode.");
        IsDead = true;
        GetComponent<PlayerMovementComponent>().enabled = false;
        GetComponent<PlayerLookComponent>().SwitchCamera("SpectatorCamera");
        GetComponent<PlayerSpectateComponent>().enabled = true; // Enable spectate component for the local player
        GetComponent<PlayerSpectateComponent>().ScrollPlayers(1); // Start spectating the next player
        GetComponent<PlayerInventoryComponent>().SetInventorySlot(-1); // Clear the inventory slot
        GetComponent<PlayerInventoryComponent>().enabled = false; // Disable inventory component for the local player
        GetComponent<PlayerInteractComponent>().enabled = false; // Disable interact component for the local player
        GetComponent<Collider>().enabled = false; // Disable the player's collider to prevent further interactions
        GetComponent<Rigidbody>().isKinematic = true; // Make the player's rigidbody kinematic to prevent physics interactions
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false; // Disable all child colliders
        }
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false; // Disable all child mesh renderers
        }
        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var skinnedRenderer in skinnedRenderers)
        {
            skinnedRenderer.enabled = false; // Disable all child skinned mesh renderers
        }
        onPlayerDeathEvent?.Invoke();
        CmdNotifyDeath(); // Notify the server about the player's death
    }

    [Command(requiresAuthority = false)]
    public void CmdNotifyDeath()
    {
        (NetworkManager.singleton as CustomNetworkManager).NotifyPlayerDeath();
    }
}