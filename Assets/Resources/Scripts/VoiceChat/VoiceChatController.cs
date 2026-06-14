using Adrenak.UniVoice;
using Mirror;
using UnityEngine;

public class VoiceChatController: MonoBehaviour
{
    public void Start()
    {
        PlayerVoiceProxyComponent.ClientSession.Client.UpdateVoiceSettings(s => s.SetMyTag("alive", true));
        PlayerVoiceProxyComponent.ClientSession.Client.UpdateVoiceSettings(s => s.SetDeafenedTag("spectator", true));
    }

    public static void ChangeVoiceChat()
    {
        PlayerVoiceProxyComponent.ClientSession.Client.UpdateVoiceSettings(s => s.SetMutedTag("alive", true));
    }
}
