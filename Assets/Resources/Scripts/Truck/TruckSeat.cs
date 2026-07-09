using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

public class TruckSeat : Interactable
{
    public Action<GameObject, GameObject> onOccupantChanged;
    [SerializeField] Transform _occupantPosition;
    [SerializeField] Transform _exitPosition;
    [SerializeField] bool _isDriverSeat = true;
    [SerializeField] InputActionReference _getUpInputActionReference;
    [SyncVar(hook = nameof(OnOccupantChanged))]
    GameObject _occupant;
    static List<GameObject> PlayersInTruck = new List<GameObject>();
    private bool _canGetUp = true;
    public bool CanGetUp { get => _canGetUp; set => _canGetUp = value; }

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
            if (NetworkClient.connection.identity != null && oldOccupant == NetworkClient.connection.identity.gameObject)
            {
                SetPlayerInput(oldOccupant, false);
                RaycastHit hit;
                if (Physics.Raycast(_exitPosition.position + Vector3.up, Vector3.down, out hit, 20f))
                {
                    oldOccupant.transform.position = hit.point + Vector3.up;
                }
                else
                {
                    oldOccupant.transform.position = _exitPosition.position;
                }
                if (oldOccupant.TryGetComponent<PlayerAnimationComponent>(out var animOld)) animOld.ResetToNormalState();
            }
            SetPlayerCollidersEnabled(oldOccupant, false);
            oldOccupant.GetComponent<Rigidbody>().isKinematic = false;
            oldOccupant.GetComponent<StepPlayer>().enabled = true;
            oldOccupant.transform.parent = null;
            oldOccupant.transform.rotation = Quaternion.identity;

            if (isServer)
            {
                oldOccupant.GetComponent<PlayerDeathComponent>().onPlayerDeathServerEvent -= PlayerDied;
            }
        }

        if (newOccupant != null)
        {
            PlayersInTruck.Add(newOccupant);
            if (NetworkClient.connection.identity != null && newOccupant == NetworkClient.connection.identity.gameObject)
            {
                SetPlayerInput(newOccupant, true);
                if (newOccupant.TryGetComponent<PlayerAnimationComponent>(out var anim)) anim.SetSittingState(_isDriverSeat);
            }
            SetPlayerCollidersEnabled(newOccupant, true);
            newOccupant.GetComponent<StepPlayer>().enabled = false;
            newOccupant.GetComponent<Rigidbody>().isKinematic = true;
            newOccupant.transform.parent = _occupantPosition;
            newOccupant.transform.localPosition = Vector3.zero;
            newOccupant.transform.localRotation = Quaternion.identity;

            if (isServer)
            {
                newOccupant.GetComponent<PlayerDeathComponent>().onPlayerDeathServerEvent += PlayerDied;
            }
        }

        onOccupantChanged?.Invoke(oldOccupant, newOccupant);
    }

    void PlayerDied()
    {
        if (_occupant != null)
        {
            _occupant = null;
        }
    }

    public override void ServerInteract(GameObject interactor)
    {
        if (_occupant != null) return;
        if (PlayersInTruck.Contains(interactor)) return;
        _occupant = interactor;
    }

    void SetPlayerInput(GameObject player, bool isOnTruck)
    {
        if (player.TryGetComponent<PlayerMovementComponent>(out var pm)) pm.enabled = !isOnTruck;
        if (player.TryGetComponent<PlayerJumpComponent>(out var pj)) pj.enabled = !isOnTruck;
        if (_isDriverSeat && player.TryGetComponent<PlayerItemUseComponent>(out var piu)) piu.enabled = !isOnTruck;
        if (_isDriverSeat && player.TryGetComponent<PlayerInventoryComponent>(out var pic))
        {
            pic.SetInventorySlot(-1);
            pic.enabled = !isOnTruck;
        }
        if (isOnTruck)
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
        foreach (var col in player.GetComponentsInChildren<Collider>()) col.enabled = !isOnTruck;
    }

    private void OnGetUpPerformed(InputAction.CallbackContext context)
    {
        if (!_canGetUp || _occupant == null) return;
        CmdGetUp();
    }

    [Command(requiresAuthority = false)]
    void CmdGetUp() => _occupant = null;
}