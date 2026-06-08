using Mirror;
using UnityEngine;

[RequireComponent(typeof(AddressComponent))]
public class DoorInteractionComponent : Interactable
{
    DoorPackageDetectionComponent _packageDetectionComponent;
    void Awake()
    {
        _packageDetectionComponent = GetComponent<DoorPackageDetectionComponent>();
        if(_packageDetectionComponent == null)
        {
            _packageDetectionComponent = GetComponentInChildren<DoorPackageDetectionComponent>();
        }
        if(_packageDetectionComponent == null)
        {
            Debug.LogError("Door is missing a PackageDetectionComponent, please add one to the door or its children.");
        }
    }
    public override void ServerInteract(GameObject interactor)
    {
        Debug.Log($"Door {gameObject.name} has been interacted with by {interactor.name}. Checking for stored package value.");
        if(_packageDetectionComponent.StoredValue > 0f)
        {
            _packageDetectionComponent.CanLoseValue = false;
            MoneyManager.Instance.ServerAddMoney(_packageDetectionComponent.StoredKnockValue);
            NetworkServer.Destroy(_packageDetectionComponent.StoredPackage);
        }
    }
    public override void ClientInteraction(GameObject interactor)
    {

    }
}