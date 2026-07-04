using Mirror;
using UnityEngine;

[RequireComponent(typeof(PlayerVoiceLink))]
[RequireComponent(typeof(PlayerDeathComponent))]
public class PlayerVoiceDeathComponent : PlayerComponent
{
    PlayerVoiceLink _playerVoiceLink;
    PlayerDeathComponent _deathComponent;

    [SyncVar(hook = nameof(OnSpectatorStatusChanged))] public bool isSpectator = false;

    void Awake()
    {
        _deathComponent = GetComponent<PlayerDeathComponent>();
        _playerVoiceLink = GetComponent<PlayerVoiceLink>();
    }

    void OnEnable()
    {
        _deathComponent.onPlayerDeathEvent += CmdNotifyDeathOnNetwork;
    }

    void OnDisable()
    {
        _deathComponent.onPlayerDeathEvent -= CmdNotifyDeathOnNetwork;
    }

    [Command]
    public void CmdNotifyDeathOnNetwork()
    {
        isSpectator = true;
    }

    void OnSpectatorStatusChanged(bool oldValue, bool newValue)
    {
        if (newValue && _playerVoiceLink.voiceId != -1)
        {
            VoiceChatController.Instance.SetSpectatorState(_playerVoiceLink.voiceId, true);
        }
    }
}
