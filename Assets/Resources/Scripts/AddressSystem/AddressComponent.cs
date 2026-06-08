using System.Collections.Generic;
using UnityEngine;

public class AddressComponent : MonoBehaviour
{
    [SerializeField] AddressInfo _address;
    public AddressInfo Address => _address;
    bool _isValidAddress = false;
    public bool IsValidAddress => _isValidAddress;
    public bool MatchesAddress(AddressInfo address)
    {
        return _address.streetName == address.streetName && _address.number == address.number;
    }
    public void SetAddress(AddressInfo address)
    {
        _address = address;
    }
}