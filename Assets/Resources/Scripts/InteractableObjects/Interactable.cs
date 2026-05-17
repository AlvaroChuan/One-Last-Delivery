using UnityEngine;
using Mirror;

public abstract class Interactable : NetworkBehaviour
{
    [Command(requiresAuthority = false)]
    public void CmdInteract(NetworkIdentity interactorIdentity)
    {
        Interact(interactorIdentity.gameObject);
    }
    public abstract void Interact(GameObject interactor);
}