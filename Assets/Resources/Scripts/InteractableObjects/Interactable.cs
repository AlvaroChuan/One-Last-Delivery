using UnityEngine;
using Mirror;

public abstract class Interactable : NetworkBehaviour
{
    [Command(requiresAuthority = false)]
    public void CmdInteract(NetworkIdentity interactorIdentity)
    {
        ServerInteract(interactorIdentity.gameObject);
    }
    public void LocalInteract(GameObject interactor)
    {
        LocalInteraction(interactor);
    }

    /// <summary>
    /// This method is called on the server when a player interacts with this object. The interactor parameter is the GameObject that is interacting with this object.
    /// </summary>
    /// <param name="interactor"></param>
    public abstract void ServerInteract(GameObject interactor);

    /// <summary>
    /// This method is called on the client when a player interacts with this object. The interactor parameter is the GameObject that is interacting with this object. This can be used to play local effects, animations, etc. that should not be handled by the server.
    /// </summary>
    /// <param name="interactor"></param>
    public virtual void LocalInteraction(GameObject interactor)
    {
        // Default implementation - can be overridden by derived classes
    }
}