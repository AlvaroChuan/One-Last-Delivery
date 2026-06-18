using Mirror;
using UnityEngine;

[RequireComponent(typeof(FieldOfViewDetector))]
public class Specter : BasicEnemy
{
    private FieldOfViewDetector _fieldOfViewDetector;
    bool _isInFOV = false;
    int _inFOVCount = 0;
    bool _checkedAfterLeavingFOV = false;

    private void Awake()
    {
        _fieldOfViewDetector = GetComponent<FieldOfViewDetector>();
    }

    protected override void Update()
    {
        CheckFOV();
        if(!isServer) return;

        if (_inFOVCount > 0)
        {
            _checkedAfterLeavingFOV = false;
            if (_chaseBehaviour.IsChasing)
            {
                _chaseBehaviour.StopChasing();
            }
            return;
        }
        else if (_inFOVCount <= 0 && !_checkedAfterLeavingFOV)
        {
            _checkedAfterLeavingFOV = true;
            CheckForPlayer();
        }

        base.Update();
    }

    void CheckFOV()
    {
        if (PlayerDead())
        {
            if (_isInFOV)
            {
                _isInFOV = false;
                CmdExitedFOV();
            }
            return;
        }

        if (_isInFOV != _fieldOfViewDetector.IsInFOV())
        {
            _isInFOV = _fieldOfViewDetector.IsInFOV();
            if (_isInFOV)
            {
                CmdEnteredFOV();
            }
            else
            {
                CmdExitedFOV();
            }
        }
    }

    bool PlayerDead()
    {
        if(NetworkClient.connection == null || NetworkClient.connection.identity == null)
        {
            return true; // Consider player dead if we can't find the player object
        }
        GameObject player = NetworkClient.connection.identity?.gameObject;
        if (player == null)
        {
            DevLogger.LogError("Player object is null. Ensure the player is connected and has a valid NetworkIdentity.");
            return true; // Consider player dead if we can't find the player object
        }

        PlayerDeathComponent playerDeathComponent = player.GetComponent<PlayerDeathComponent>();
        if (playerDeathComponent == null)
        {
            DevLogger.LogError("PlayerDeathComponent is missing on the player object.");
            return true; // Consider player dead if the component is missing
        }

        return playerDeathComponent.IsDead;
    }

    [Command(requiresAuthority = false)]
    void CmdEnteredFOV()
    {
        _inFOVCount++;
    }
    [Command(requiresAuthority = false)]
    void CmdExitedFOV()
    {
        _inFOVCount--;
    }
}