using UnityEngine;
using Mirror;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AddressComponent))]
public class PackageAddressAssignmentComponent : NetworkBehaviour
{
    [SerializeField] bool _clearUsedAddressesOnAwake = true;
    static List<AddressInfo> UsedAddresses = new List<AddressInfo>();
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
        string libraryPath = AddressLibrary.GetPath();
        string resourcePath = libraryPath.Substring("Assets/Resources/".Length, libraryPath.Length - "Assets/Resources/".Length - ".asset".Length);
        AddressLibrary addressLibrary = Resources.Load<AddressLibrary>(resourcePath);
        if (addressLibrary == null || addressLibrary.validAddresses.Count == 0)
        {
            Debug.LogError("Address library not found or empty. Please generate the address library for the current scene.");
            return;
        }
        AddressInfo newAddress;
        do
        {
            newAddress = addressLibrary.validAddresses[Random.Range(0, addressLibrary.validAddresses.Count)];
        }
        while (IsAddressUsed(newAddress));

        UsedAddresses.Add(newAddress);
        _addressComponent.SetAddress(newAddress);
    }

    [ClientRpc]
    void RpcUpdateAddressDisplay(string streetName, int number)
    {
        _addressComponent.SetAddress(new AddressInfo { streetName = streetName, number = number });
        // Update any visual display of the address here, such as a text mesh or UI element on the package.
    }

    bool IsAddressUsed(AddressInfo address)
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