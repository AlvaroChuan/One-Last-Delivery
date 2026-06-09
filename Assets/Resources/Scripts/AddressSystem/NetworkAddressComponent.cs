using UnityEngine;
using Mirror;
using Unity.VisualScripting;
using System;

public class NetworkAddressComponent : NetworkBehaviour, IAddress
{
    [SerializeField, SyncVar(hook = nameof(ClientUpdateAddress))] AddressInfo _address;
    public AddressInfo Address => _address;
    /// <summary>
    /// Event that is invoked on clients when the address changes. The event provides both the old and new address values for reference.
    /// Use to update visual representations of the address on clients when it changes on the server.
    /// </summary>
    public Action<AddressInfo, AddressInfo> onAddressChanged;
    public bool MatchesAddress(AddressInfo address)
    {
        return _address.streetName == address.streetName && _address.number == address.number;
    }
    [Server]
    public void SetAddress(AddressInfo address)
    {
        _address = address;
    }
    void ClientUpdateAddress(AddressInfo oldAddress, AddressInfo newAddress)
    {
        onAddressChanged?.Invoke(oldAddress, newAddress);
    }
}