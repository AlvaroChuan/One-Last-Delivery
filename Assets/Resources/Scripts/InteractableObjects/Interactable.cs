using UnityEngine;
using Mirror;

public abstract class Interactable : NetworkBehaviour
{
    [Command(requiresAuthority = false)]
    public void CmdInteract(NetworkIdentity interactorIdentity)
    {
        Interact(interactorIdentity.gameObject);
    }
    public void LocalInteract(GameObject interactor)
    {
        LocalInteraction(interactor);
    }

    /// <summary>
    /// This method is called on the server when a player interacts with this object. The interactor parameter is the GameObject that is interacting with this object.
    /// </summary>
    /// <param name="interactor"></param>
    public abstract void Interact(GameObject interactor);

    public virtual void LocalInteraction(GameObject interactor)
    {
        // Default implementation - can be overridden by derived classes
    }
}