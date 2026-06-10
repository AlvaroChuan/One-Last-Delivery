using System.Collections;
using Adrenak.UniVoice.Samples;
using Mirror;
using UnityEngine;

public class PlayerVoiceDeathComponent : PlayerComponent
{
    PlayerVoiceProxyComponent _playerVoiceProxyComponent;

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

    private void Start()
    {
        _playerVoiceProxyComponent = FindObjectOfType<PlayerVoiceProxyComponent>();
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
