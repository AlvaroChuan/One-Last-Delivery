using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Linq;
using Mirror.Examples.Basic;

public class PlayerSpectateComponent : InputComponent
{
    [SerializeField] private InputActionReference _spectateActionReference;
    private CinemachineCamera _cinemachineCamera;
    private int _currentPlayerIndex = 0;

    void Awake()
    {
        _cinemachineCamera = GameObject.FindGameObjectWithTag("SpectatorCamera").GetComponent<CinemachineCamera>();
    }
    protected override void BindInputs()
    {
        _spectateActionReference.action.Enable();
        _spectateActionReference.action.performed += OnSpectateInput;
    }

    protected override void UnbindInputs()
    {
        _spectateActionReference.action.Disable();
        _spectateActionReference.action.performed -= OnSpectateInput;
    }

    void OnSpectateInput(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        if (context.ReadValue<float>() > 0)
        {
            ScrollPlayers(1); // Scroll forward
        }
        else if (context.ReadValue<float>() < 0)
        {
            ScrollPlayers(-1); // Scroll backward
        }
    }

    public void ScrollPlayers(int sign)
    {
        GameObject[] _players = PlayerRegistry.SpawnedPlayers.ToArray();

        DevLogger.Log("Players found: " + _players.Length);

        PlayerDeathComponent deathComponent;

        int attempts = 0;

        do
        {
            _currentPlayerIndex += sign;
            if (_currentPlayerIndex < 0)
                _currentPlayerIndex = _players.Length - 1;
            else if (_currentPlayerIndex >= _players.Length)
                _currentPlayerIndex = 0;

            deathComponent = _players[_currentPlayerIndex].GetComponent<PlayerDeathComponent>();

            DevLogger.Log($"Attempting to spectate player {_players[_currentPlayerIndex].name}. IsDead: {deathComponent.IsDead}");

            attempts++;
        }
        while (deathComponent.IsDead && attempts < _players.Length);

        if (deathComponent.IsDead)
        {
            DevLogger.Log("No alive players to spectate.");
            return;
        }

        _cinemachineCamera.Follow = _players[_currentPlayerIndex].GetComponent<PlayerLookComponent>().Eyes;
        _cinemachineCamera.LookAt = _players[_currentPlayerIndex].GetComponent<PlayerLookComponent>().Eyes;
    }
}