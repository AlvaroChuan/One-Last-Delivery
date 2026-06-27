using System;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(LocalAddressComponent))]
public class EvilDoorInteractionComponent : Interactable
{
    public struct EvilDoorInteractionInfo
    {
        public bool isSuccessful;
    }
    public Action<EvilDoorInteractionInfo> onEvilDoorInteractionEvent;
    LocalAddressComponent _localAddressComponent;

    void Awake()
    {
        _localAddressComponent = GetComponent<LocalAddressComponent>();
        if (_localAddressComponent == null)
        {
            DevLogger.LogError("Door is missing a LocalAddressComponent, please add one to the door.");
        }
    }

    public override void ServerInteract(GameObject interactor)
    {
        GameObject package = interactor.GetComponent<PlayerInventoryComponent>()?.CarriedPackage;
        if (package == null) return;

        NetworkAddressComponent packageAddressComponent = package.GetComponent<NetworkAddressComponent>();
        if (packageAddressComponent == null)
        {
            DevLogger.LogError("Package is missing a NetworkAddressComponent, please add one to the package.");
            return;
        }

        if (packageAddressComponent.MatchesAddress(_localAddressComponent.Address))
        {
            PlayerHealthComponent playerHealth = interactor.GetComponent<PlayerHealthComponent>();
            if (playerHealth != null)
            {
                playerHealth.RpcTakeDamage(playerHealth.MaxHealth); // Kill the player
            }
        }
    }

    [ClientRpc]
    public void RpcDoorInteraction(bool isSuccessful)
    {
        onEvilDoorInteractionEvent?.Invoke(new EvilDoorInteractionInfo
        {
            isSuccessful = isSuccessful
        });
    }
}