using UnityEngine;
using Mirror;

public abstract class Interactable : NetworkBehaviour
{
    public abstract void Interact(GameObject interactor);
}