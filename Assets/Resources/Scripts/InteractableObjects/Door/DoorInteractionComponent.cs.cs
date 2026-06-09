using Mirror;
using UnityEngine;

[RequireComponent(typeof(LocalAddressComponent))]
public class DoorInteractionComponent : Interactable
{
    LocalAddressComponent _localAddressComponent;

    void Awake()
    {
        _localAddressComponent = GetComponent<LocalAddressComponent>();
        if (_localAddressComponent == null)
        {
            Debug.LogError("Door is missing a LocalAddressComponent, please add one to the door.");
        }
    }

    public override void ServerInteract(GameObject interactor)
    {
        GameObject package = interactor.GetComponent<PlayerInventoryComponent>()?.CarriedPackage;
        if (package == null) return;

        NetworkAddressComponent packageAddressComponent = package.GetComponent<NetworkAddressComponent>();
        if (packageAddressComponent == null)
        {
            Debug.LogError("Package is missing a NetworkAddressComponent, please add one to the package.");
            return;
        }

        if (packageAddressComponent.MatchesAddress(_localAddressComponent.Address))
        {
            MoneyManager.Instance.ServerAddMoney(package.GetComponent<PackageValueComponent>()?.GetValue() ?? 0);
            NetworkServer.Destroy(package);
        }
    }
}