using Mirror;

public class PlayerVoiceID : NetworkBehaviour
{
    [SyncVar] public int univoicePeerId;

    public override void OnStartServer()
    {
        univoicePeerId = connectionToClient.connectionId;
    }
}
