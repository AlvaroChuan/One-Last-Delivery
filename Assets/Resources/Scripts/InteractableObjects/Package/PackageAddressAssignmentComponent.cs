using UnityEngine;
using Mirror;
using System.Collections.Generic;
using Unity.VisualScripting;

[RequireComponent(typeof(AddressComponent))]
public class PackageAddressAssignmentComponent : NetworkBehaviour
{
    [SerializeField] bool _clearUsedAddressesOnAwake = true;
    static List<AddressComponent.AddressInfo> UsedAddresses = new List<AddressComponent.AddressInfo>();
    private AddressComponent _addressComponent;

    private void Awake()
    {
        _addressComponent = GetComponent<AddressComponent>();
        if (_clearUsedAddressesOnAwake)
        {
            UsedAddresses.Clear();
        }
    }

    override public void OnStartServer()
    {
        base.OnStartServer();
        AssignRandomAddress();
    }

    [Server]
    public void AssignRandomAddress()
    {
        AddressComponent.AddressInfo newAddress;
        do
        {
            newAddress = AddressComponent.ValidAddresses[Random.Range(0, AddressComponent.ValidAddresses.Count)];
        }
        while (IsAddressUsed(newAddress));

        UsedAddresses.Add(newAddress);
        _addressComponent.SetAddress(newAddress);
    }

    [ClientRpc]
    void RpcUpdateAddressDisplay(string streetName, int number)
    {
        _addressComponent.SetAddress(new AddressComponent.AddressInfo { streetName = streetName, number = number });
        // Update any visual display of the address here, such as a text mesh or UI element on the package.
    }

    bool IsAddressUsed(AddressComponent.AddressInfo address)
    {
        foreach (var usedAddress in UsedAddresses)
        {
            if (usedAddress.streetName == address.streetName && usedAddress.number == address.number)
            {
                return true;
            }
        }
        return false;
    }
}