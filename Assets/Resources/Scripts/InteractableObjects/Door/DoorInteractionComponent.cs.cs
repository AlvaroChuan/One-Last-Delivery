using System;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(LocalAddressComponent))]
public class DoorInteractionComponent : Interactable
{
    public struct DoorInteractionInfo
    {
        public bool isSuccessful;
    }
    public Action<DoorInteractionInfo> onDoorInteractionEvent;
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
            PackageValueComponent packageValueComponent = package.GetComponent<PackageValueComponent>();
            BalanceManager.RegisterTransaction("Package delivered", packageValueComponent.GetValue());
            float penalty = packageValueComponent.GetPenalty();
            if (penalty > 0f)
            {
                BalanceManager.RegisterTransaction("Package was damaged", -penalty);
            }
            NetworkServer.Destroy(package);
            RpcDoorInteraction(true);
        }
        else
        {
            RpcDoorInteraction(false);
        }
    }

    [ClientRpc]
    public void RpcDoorInteraction(bool isSuccessful)
    {
        onDoorInteractionEvent?.Invoke(new DoorInteractionInfo
        {
            isSuccessful = isSuccessful
        });
    }
}