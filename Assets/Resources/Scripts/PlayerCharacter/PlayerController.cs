using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovementComponent))]
[RequireComponent(typeof(SpectatorMovementComponent))]
[RequireComponent(typeof(PlayerLookComponent))]
public class PlayerController : NetworkBehaviour
{
    private PlayerMovementComponent _movementComponent;
    private SpectatorMovementComponent _spectatorComponent;
    private PlayerLookComponent _lookComponent;

    private bool _alive = true;

    void Awake()
    {
        _movementComponent = GetComponent<PlayerMovementComponent>();
        _lookComponent = GetComponent<PlayerLookComponent>();
        _spectatorComponent = GetComponent<SpectatorMovementComponent>();
    }

    void Start()
    {
        _spectatorComponent.enabled = false; // Start with spectator movement disabled
        if (!isLocalPlayer)
        {
            Camera playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
            }
        }
    }

    public void Die()
    {
        RpcDie();
    }

    [ClientRpc]
    public void RpcDie()
    {
        if (!isLocalPlayer) return;

        Debug.Log("Player has died. Switching to spectator mode.");
        _alive = false;
        _movementComponent.enabled = false;
        _spectatorComponent.enabled = true;
    }
}