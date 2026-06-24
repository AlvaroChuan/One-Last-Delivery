using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using System.Collections.Generic;
using System;

public class TruckSeat : Interactable
{
    [SerializeField] Transform _occupantPosition;
    [SerializeField] Transform _exitPosition;
    [SerializeField] bool _isDriverSeat = true;
    [SerializeField] InputActionReference _getUpInputActionReference;
    [SyncVar(hook = nameof(OnOccupantChanged))]
    GameObject _occupant;
    Quaternion _lastRotation;

    private void OnOccupantChanged(GameObject oldOccupant, GameObject newOccupant)
    {
        if (isServer)
        {
            if (newOccupant != null)
            {
                netIdentity.RemoveClientAuthority();
                netIdentity.AssignClientAuthority(newOccupant.GetComponent<NetworkIdentity>().connectionToClient);
            }
            else
            {
                netIdentity.RemoveClientAuthority();
            }
        }

        if (oldOccupant != null)
        {
            if(NetworkClient.connection.identity != null && oldOccupant == NetworkClient.connection.identity.gameObject)
            {
                // If the old occupant is the local player, re-enable their input and colliders
                SetPlayerInput(oldOccupant, false);
            }
            SetPlayerCollidersEnabled(oldOccupant, false); // Re-enable colliders for the old occupant
        }
        if (newOccupant != null)
        {
            if(NetworkClient.connection.identity != null && newOccupant == NetworkClient.connection.identity.gameObject)
            {
                // If the new occupant is the local player, disable their input and colliders
                SetPlayerInput(newOccupant, true);
            }
            SetPlayerCollidersEnabled(newOccupant, true); // Disable colliders for the new occupant
            netIdentity.AssignClientAuthority(newOccupant.GetComponent<NetworkIdentity>().connectionToClient);
        }
    }

    public override void ServerInteract(GameObject interactor)
    {
        if (!_isDriverSeat) return; // Seat is already occupied

        _occupant = interactor;
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

    void SetPlayerInput(GameObject player, bool isOnTruck)
    {
        if (player.TryGetComponent<PlayerMovementComponent>(out var playerMovementComponent))
        {
            playerMovementComponent.enabled = !isOnTruck;
        }
        if (player.TryGetComponent<PlayerJumpComponent>(out var playerJumpComponent))
        {
            playerJumpComponent.enabled = !isOnTruck;
        }
        if (_isDriverSeat && player.TryGetComponent<PlayerItemUseComponent>(out var playerItemUseComponent))
        {
            playerItemUseComponent.enabled = !isOnTruck;
        }
        if (_isDriverSeat && player.TryGetComponent<PlayerInventoryComponent>(out var playerInventoryComponent))
        {
            playerInventoryComponent.enabled = !isOnTruck;
        }
        if(isOnTruck)
        {
            _getUpInputActionReference.action.performed += OnGetUpPerformed;
            _getUpInputActionReference.action.Enable();

            PlayerInteractComponent playerInteractComponent = player.GetComponent<PlayerInteractComponent>();
            if (playerInteractComponent != null)
            {
                playerInteractComponent.onInteractEvent += OnPlayerInteract;
            }
        }
        else
        {
            _getUpInputActionReference.action.performed -= OnGetUpPerformed;
            _getUpInputActionReference.action.Disable();

            PlayerInteractComponent playerInteractComponent = player.GetComponent<PlayerInteractComponent>();
            if (playerInteractComponent != null)
            {
                playerInteractComponent.onInteractEvent -= OnPlayerInteract;
            }
        }
    }

    void SetPlayerCollidersEnabled(GameObject player, bool isOnTruck)
    {
        Collider[] colliders = player.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = !isOnTruck;
        }
    }

    private void OnGetUpPerformed(InputAction.CallbackContext context)
    {
        if (_occupant == null) return; // No occupant to get up

        GetUp();
    }

    void GetUp()
    {
        _occupant = null;
    }
}