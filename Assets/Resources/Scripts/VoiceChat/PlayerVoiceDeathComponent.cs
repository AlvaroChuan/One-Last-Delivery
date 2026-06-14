using Mirror;
using UnityEngine;

[RequireComponent(typeof(PlayerDeathComponent))]
public class PlayerVoiceDeathComponent : PlayerComponent
{
    PlayerVoiceProxyComponent _playerVoiceProxyComponent;
    PlayerDeathComponent _deathComponent;

    [SyncVar]
    public int voiceId = -1;

    [SyncVar]
    public bool isAlive = true;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (connectionToClient != null)
        {
            voiceId = connectionToClient.connectionId;
        }
    }

    void Awake()
    {
        _deathComponent = GetComponent<PlayerDeathComponent>();
    }

    private void Start()
    {
        _playerVoiceProxyComponent = FindAnyObjectByType<PlayerVoiceProxyComponent>();
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
        isAlive = false;
        RpcAllPlayersUpdateAudio();
    }

    [ClientRpc]
    private void RpcAllPlayersUpdateAudio()
    {
        if (_playerVoiceProxyComponent != null)
            _playerVoiceProxyComponent.UpdateAudio();
    }
}
