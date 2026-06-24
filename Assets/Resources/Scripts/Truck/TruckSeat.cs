using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using System.Collections.Generic;

public class TruckSeat : Interactable
{
    [SerializeField] Transform _occupantPosition;
    [SerializeField] Transform _exitPosition;
    [SerializeField] bool _isDriverSeat = true;
    [SerializeField] InputActionReference _getUpInputActionReference;
    GameObject _occupant;
    Quaternion _lastRotation;
    public override void ServerInteract(GameObject interactor)
    {
        if (!_isDriverSeat) return; // Seat is already occupied

        NetworkIdentity interactorIdentity = interactor.GetComponent<NetworkIdentity>();

        netIdentity.RemoveClientAuthority();
        netIdentity.AssignClientAuthority(interactorIdentity.connectionToClient);
    }

    public override void ClientInteraction(GameObject interactor)
    {
        if (_occupant != null) return; // Seat is already occupied

        _occupant = interactor;
        _occupant.transform.position = _occupantPosition.position;

        _getUpInputActionReference.action.Enable();
        _getUpInputActionReference.action.performed += OnGetUpPerformed;

        if (_occupant.TryGetComponent<PlayerInteractComponent>(out var playerInteractComponent))
        {
            playerInteractComponent.onInteractEvent += OnPlayerInteract;
        }

        SetPlayerInputEnabled(_occupant, false); // Disable player input while seated
    }

    private void OnPlayerInteract(PlayerInteractComponent.InteractInfo info)
    {
        if (info.interactable is TruckSeat && info.interactable != this)
        {
            GetUp(); // If the player interacts with another seat, get up from the current seat
        }
    }

    void Update()
    {
        if (_occupant != null)
        {
            _occupant.transform.position = _occupantPosition.position;
            Vector3 rotationDelta = transform.rotation.eulerAngles - _lastRotation.eulerAngles;
            _occupant.transform.Rotate(rotationDelta, Space.World); // Apply the rotation difference of the seat to the occupant
            _occupant.transform.rotation = Quaternion.LookRotation(_occupant.transform.forward, transform.up);
        }
        _lastRotation = transform.rotation;
    }

    void SetPlayerInputEnabled(GameObject player, bool enabled)
    {
        if (player.TryGetComponent<PlayerMovementComponent>(out var playerMovementComponent))
        {
            playerMovementComponent.enabled = enabled;
        }
        if (player.TryGetComponent<PlayerJumpComponent>(out var playerInteractComponent))
        {
            playerInteractComponent.enabled = enabled;
        }
        if (_isDriverSeat && player.TryGetComponent<PlayerItemUseComponent>(out var playerItemUseComponent))
        {
            playerItemUseComponent.enabled = enabled;
        }
        if (_isDriverSeat && player.TryGetComponent<PlayerInventoryComponent>(out var playerInventoryComponent))
        {
            playerInventoryComponent.enabled = enabled;
        }

        List<Collider> playerColliders = new List<Collider>(_occupant.GetComponents<Collider>());
        playerColliders.AddRange(_occupant.GetComponentsInChildren<Collider>());
        foreach (var collider in playerColliders)
        {
            collider.enabled = enabled; // Re-enable the player's colliders
        }
    }

    private void OnGetUpPerformed(InputAction.CallbackContext context)
    {
        if (_occupant == null) return; // No occupant to get up

        SetPlayerInputEnabled(_occupant, true); // Re-enable player input
        GetUp();

        if (_isDriverSeat)
        {
            CmdOnGetUp();
        }
    }

    void GetUp()
    {
        PlayerInteractComponent playerInteractComponent = _occupant.GetComponent<PlayerInteractComponent>();
        if (playerInteractComponent != null)
        {
            playerInteractComponent.onInteractEvent -= OnPlayerInteract;
        }

        _occupant.transform.position = _exitPosition.position;
        _occupant.transform.rotation = _exitPosition.rotation;
        _occupant.transform.up = Vector3.up; // Ensure the player is upright when getting up

        _occupant = null;

        _getUpInputActionReference.action.performed -= OnGetUpPerformed;
        _getUpInputActionReference.action.Disable();
    }

    [Command]
    void CmdOnGetUp()
    {
        netIdentity.RemoveClientAuthority();
    }
}