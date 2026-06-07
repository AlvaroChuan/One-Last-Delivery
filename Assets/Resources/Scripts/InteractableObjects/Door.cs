using UnityEngine;

public class Door : Interactable
{
    [SerializeField] Address _address;
    public override void ServerInteract(GameObject interactor)
    {
        //Debug.Log($"Interacting with door at {_address.StreetName} {_address.Number}");
    }
    public override void LocalInteraction(GameObject interactor)
    {
        //Debug.Log($"Locally interacting with door at {_address.StreetName} {_address.Number}");
    }
}