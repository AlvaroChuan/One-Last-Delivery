using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class TruckHorn : NetworkBehaviour
{
    [SerializeField] private TruckSeat _driverSeat;
    [SerializeField] private AudioEvent _hornAudioEvent;
    [SerializeField] private InputActionReference _hornInputAction;

    void OnEnable()
    {
        _driverSeat.onOccupantChanged += HandleOccupantChanged;
    }
    void OnDisable()
    {
        _driverSeat.onOccupantChanged -= HandleOccupantChanged;
    }

    void HandleOccupantChanged(GameObject oldOccupant, GameObject newOccupant)
    {
        if (newOccupant == NetworkClient.connection.identity.gameObject)
        {
            _hornInputAction.action.Enable();
            _hornInputAction.action.performed += HandleHornInput;
        }
        else if (oldOccupant == NetworkClient.connection.identity.gameObject)
        {
            _hornInputAction.action.performed -= HandleHornInput;
            _hornInputAction.action.Disable();
        }
    }

    private void HandleHornInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            CmdPlayHorn();
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdPlayHorn()
    {
        RpcPlayHorn();
    }

    [ClientRpc]
    private void RpcPlayHorn()
    {
        _hornAudioEvent.Play(gameObject);
    }
}