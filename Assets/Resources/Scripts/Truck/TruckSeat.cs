using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

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
            if(NetworkClient.connection.identity != null && oldOccupant == NetworkClient.localPlayer)
            {
                // If the old occupant is the local player, re-enable their input and colliders
                SetPlayerInput(oldOccupant, false);
                RaycastHit hit;
                if (Physics.Raycast(_exitPosition.position + Vector3.up * 10f, Vector3.down, out hit, 20f))
                {
                    DevLogger.Log($"Moving player {oldOccupant.name} to ground at {hit.point}");
                    oldOccupant.transform.position = hit.point + Vector3.up; // Move the player to the ground below them
                }
                else
                {
                    DevLogger.Log($"No ground found below exit position for player {oldOccupant.name}, moving to exit position");
                    oldOccupant.transform.position = _exitPosition.position; // Move the player to the exit position if no ground is found
                }
            }
            SetPlayerCollidersEnabled(oldOccupant, false); // Re-enable colliders for the old occupant
            oldOccupant.GetComponent<NetworkTransformReliable>().enabled = true; // Re-enable NetworkTransform for the old occupant
            oldOccupant.transform.SetParent(null); // Unparent the old occupant from the seat position
        }
        if (newOccupant != null)
        {
            if(NetworkClient.connection.identity != null && newOccupant == NetworkClient.localPlayer)
            {
                // If the new occupant is the local player, disable their input and colliders
                SetPlayerInput(newOccupant, true);
            }
            SetPlayerCollidersEnabled(newOccupant, true); // Disable colliders for the new occupant
            newOccupant.GetComponent<NetworkTransformReliable>().enabled = false; // Disable NetworkTransform for the new occupant
            newOccupant.transform.position = _occupantPosition.position; // Move the new occupant to the seat position
            newOccupant.transform.rotation = transform.rotation; // Align the new occupant's rotation with the seat's rotation
            newOccupant.transform.SetParent(_occupantPosition); // Parent the new occupant to the seat position for movement synchronization
        }
    }

    public override void ServerInteract(GameObject interactor)
    {
        if (_occupant != null) return;

        _occupant = interactor;
    }

    private void OnPlayerInteract(PlayerInteractComponent.InteractInfo info)
    {
        if (info.interactable is TruckSeat && info.interactable != this)
        {
            CmdGetUp(); // If the player interacts with another seat, get up from the current seat
        }
    }

    void Update()
    {
        /*
        if (_occupant != null && NetworkClient.connection.identity != null && _occupant == NetworkClient.localPlayer)
        {
            _occupant.transform.position = _occupantPosition.position;
            Quaternion rotationDelta = transform.rotation * Quaternion.Inverse(_lastRotation);
            GameObject model = _occupant.GetComponent<PlayerLookComponent>().Model;
            model.transform.rotation = rotationDelta * model.transform.rotation;
            model.transform.rotation = Quaternion.LookRotation(_occupant.transform.forward, transform.up);
        }
        _lastRotation = transform.rotation;*/
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

        CmdGetUp();
    }

    [Command(requiresAuthority = false)]
    void CmdGetUp()
    {
        _occupant = null;
    }

    void OnDestroy()
    {
        if (_occupant != null && NetworkClient.connection.identity != null && _occupant == NetworkClient.localPlayer)
        {
            SetPlayerInput(_occupant, false);
            SetPlayerCollidersEnabled(_occupant, false);
        }
    }
}