using System.Collections;
using UnityEngine;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Outputs;
using Mirror;

public class ProxymityVoiceChatController : BaseVoiceChat
{
    [SerializeField] private float _distanceProximityChat = 10f;

    private void Start()
    {
        TAG = "[ProxymityVoiceChatController]";
        if (HasSetUp) {
            return;
        }
        HasSetUp = Setup();
    }

    private void OnDestroy()
    {
        StopVoiceChat();
    }

    protected override bool SetupClientSession()
    {
        base.SetupClientSession();

        _client.OnPeerJoined += id =>
        {
            StartCoroutine(ConfigureAudio(id));
        };
        return true;
    }

    private IEnumerator ConfigureAudio(int id)
    {
        if (ClientSession == null || !ClientSession.PeerOutputs.ContainsKey(id)) yield break;

        IAudioOutput peerOutput = ClientSession.PeerOutputs[id];
        StreamedAudioSourceOutput streamedAudioSourceOutput = peerOutput as StreamedAudioSourceOutput;
        AudioSource peerAudioSource = streamedAudioSourceOutput.Stream.UnityAudioSource;
        
        Transform peerAvatar = null;
        float timeout = 10f;
        while (peerAvatar == null && timeout > 0f)
        {
            peerAvatar = GetAvatarForPeerID(id);
            if (peerAvatar == null)
            {
                yield return new WaitForSeconds(0.5f);
                timeout -= 0.5f;
            }
        }

        if (peerAvatar != null)
        {
            peerAudioSource.transform.SetParent(peerAvatar);
            peerAudioSource.transform.localPosition = Vector3.zero;
        }

        peerAudioSource.rolloffMode = AudioRolloffMode.Linear;
        peerAudioSource.maxDistance = _distanceProximityChat;

        bool peerIsAlive = true;
        if (peerAvatar != null)
        {
            PlayerVoiceDeathComponent deathComp = peerAvatar.GetComponent<PlayerVoiceDeathComponent>();
            if (deathComp != null)
            {
                peerIsAlive = deathComp.isAlive;
            }
        }

        bool localIsAlive = true;
        PlayerVoiceDeathComponent localDeathComp = GetLocalPlayerDeathComponent();
        if (localDeathComp != null)
        {
            localIsAlive = localDeathComp.isAlive;
        }

        if (peerIsAlive)
        {
            peerAudioSource.mute = false;
            peerAudioSource.spatialBlend = 1f;
        }
        else if (localIsAlive)
        {
            peerAudioSource.mute = true;
        }
        else
        {
            peerAudioSource.mute = false;
            peerAudioSource.spatialBlend = 0f;
        }
    }

    public void UpdateAudio()
    {
        if (ClientSession == null) return;
        foreach (int id in ClientSession.PeerOutputs.Keys)
        {
            StartCoroutine(ConfigureAudio(id));
        }
    }

    private PlayerVoiceDeathComponent GetLocalPlayerDeathComponent()
    {
        var players = FindObjectsOfType<PlayerVoiceDeathComponent>();
        foreach (PlayerVoiceDeathComponent p in players)
        {
            if (p.isLocalPlayer) return p;
        }
        return null;
    }

    private Transform GetAvatarForPeerID(int id)
    {
        PlayerVoiceDeathComponent[] players = FindObjectsOfType<PlayerVoiceDeathComponent>();
        foreach (PlayerVoiceDeathComponent p in players)
        {
            if (p.voiceId == id) return p.transform;
        }

        if (NetworkServer.active && NetworkServer.connections.TryGetValue(id, out NetworkConnectionToClient connection))
        {
            if (connection.identity != null)
            {
                return connection.identity.transform;
            }
        }
        return null;
    }
}
