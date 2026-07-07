using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Mirror.Examples.Basic;
using System;

public class TruckSeat : Interactable
{
    public Action<GameObject, GameObject> onOccupantChanged; // Action to notify when the occupant changes, passing old and new occupant
    [SerializeField] Transform _occupantPosition;
    [SerializeField] Transform _exitPosition;
    [SerializeField] bool _isDriverSeat = true;
    [SerializeField] InputActionReference _getUpInputActionReference;
    [SyncVar(hook = nameof(OnOccupantChanged))]
    GameObject _occupant;
    static List<GameObject> PlayersInTruck = new List<GameObject>();
    private bool _canGetUp = true; // Flag to control if the player can get up from the seat
    public bool CanGetUp
    {
        get => _canGetUp;
        set => _canGetUp = value;
    }

    private void OnOccupantChanged(GameObject oldOccupant, GameObject newOccupant)
    {
        if (isServer && _isDriverSeat)
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
            PlayersInTruck.Remove(oldOccupant);
            if(NetworkClient.connection.identity != null && oldOccupant == NetworkClient.connection.identity.gameObject)
            {
                // If the old occupant is the local player, re-enable their input and colliders
                SetPlayerInput(oldOccupant, false);
                RaycastHit hit;
                if (Physics.Raycast(_exitPosition.position + Vector3.up, Vector3.down, out hit, 20f))
                {
                    oldOccupant.transform.position = hit.point + Vector3.up; // Move the player to the ground below them
                }
                else
                {
                    oldOccupant.transform.position = _exitPosition.position; // Move the player to the exit position if no ground is found
                }
            }
            SetPlayerCollidersEnabled(oldOccupant, false); // Re-enable colliders for the old occupant
            oldOccupant.GetComponent<Rigidbody>().isKinematic = false; // Make the old occupant's Rigidbody non-kinematic to allow physics interactions
            oldOccupant.transform.parent = null; // Unparent the old occupant from the seat
            oldOccupant.transform.rotation = Quaternion.identity; // Reset rotation to ensure correct orientation
        }
        if (newOccupant != null)
        {
            PlayersInTruck.Add(newOccupant);
            if(NetworkClient.connection.identity != null && newOccupant == NetworkClient.connection.identity.gameObject)
            {
                // If the new occupant is the local player, disable their input and colliders
                SetPlayerInput(newOccupant, true);
            }
            SetPlayerCollidersEnabled(newOccupant, true); // Disable colliders for the new occupant
            newOccupant.GetComponent<Rigidbody>().isKinematic = true; // Make the new occupant's Rigidbody kinematic to prevent physics interactions
            newOccupant.transform.parent = _occupantPosition; // Parent the new occupant to the seat
            newOccupant.transform.localPosition = Vector3.zero; // Reset local position to ensure correct placement
            newOccupant.transform.localRotation = Quaternion.identity; // Reset local rotation to ensure correct orientation
        }

        onOccupantChanged?.Invoke(oldOccupant, newOccupant); // Notify subscribers about the occupant change
    }

    public override void ServerInteract(GameObject interactor)
    {
        if (_occupant != null) return;

        if (PlayersInTruck.Contains(interactor))
        {
            DevLogger.Log($"Player {interactor.name} is already in a truck. Cannot occupy another seat.");
            return; // Prevent occupying another seat if the player is already in a truck
        }

        _occupant = interactor;
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
            playerInventoryComponent.SetInventorySlot(-1); // Deselect any selected inventory slot when getting on the truck
            playerInventoryComponent.enabled = !isOnTruck;
        }
        if(isOnTruck)
        {
            _getUpInputActionReference.action.performed += OnGetUpPerformed;
            _getUpInputActionReference.action.Enable();
        }
        else
        {
            _getUpInputActionReference.action.performed -= OnGetUpPerformed;
            _getUpInputActionReference.action.Disable();
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
        if(!_canGetUp) return; // Seat is not enabled, ignore input
        if (_occupant == null) return; // No occupant to get up

        CmdGetUp();
    }

    [Command(requiresAuthority = false)]
    void CmdGetUp()
    {
        _occupant = null;
    }
}