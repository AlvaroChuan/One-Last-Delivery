using System.Collections.Generic;
using UnityEngine;

public class LocalAddressComponent : MonoBehaviour, IAddress
{
    [SerializeField] AddressInfo _address;
    public AddressInfo Address => _address;
    public bool MatchesAddress(AddressInfo address)
    {
        return _address.streetName == address.streetName && _address.number == address.number;
    }
}