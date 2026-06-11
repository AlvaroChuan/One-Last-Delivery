using Mirror;

public class PlayerComponent : NetworkBehaviour
{
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isLocalPlayer)
        {
            enabled = false;
        }
    }
}