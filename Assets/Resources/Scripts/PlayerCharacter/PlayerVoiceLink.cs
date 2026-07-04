using UnityEngine;
using Mirror;

public class PlayerVoiceLink : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnVoiceIdChanged))]
    [HideInInspector] public int voiceId = -1;

    public override void OnStartClient()
    {
        if (!isLocalPlayer) return;

        if(!TryLinkVoiceId())
        {
            BaseVoiceChat.OnLocalPeerJoined += LinkVoiceId;
        }
    }

    void LinkVoiceId()
    {
        if (!TryLinkVoiceId())
        {
            Debug.unityLogger.Log(LogType.Error, "[Proxymity]", $"Failed to link voice ID for player {gameObject.name}");
        }
    }

    bool TryLinkVoiceId()
    {
        if (BaseVoiceChat.HasSetUp && BaseVoiceChat.ClientSession != null)
        {
            CmdSetVoicePeerId(BaseVoiceChat.LocalPeerId);
            return true;
        }
        return false;
    }

    [Command]
    private void CmdSetVoicePeerId(int id)
    {
        voiceId = id;
    }

    private void OnVoiceIdChanged(int oldId, int newId)
    {
        if (newId != -1)
        {
            // Tell the Proximity controller that this ID now belongs to this Transform
            VoiceChatController.Instance.RegisterPlayerVoice(newId, transform);
        }
    }

    void OnDestroy()
    {
        if (!isLocalPlayer) return;

        BaseVoiceChat.OnLocalPeerJoined -= LinkVoiceId;
    }
}