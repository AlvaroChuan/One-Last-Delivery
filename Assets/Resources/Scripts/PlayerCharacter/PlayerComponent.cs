using Mirror;
using UnityEngine;

public class PlayerComponent : NetworkBehaviour
{
    protected virtual void Start()
    {
        if (!isLocalPlayer)
        {
            enabled = false;
        }
    }
}