using System.Collections.Generic;
using UnityEngine;

public class AddressComponent : MonoBehaviour
{
    [System.Serializable]
    public struct AddressInfo
    {
        [DropdownString("StreetNames")] public string streetName;
        public int number;
    }
    public static readonly List<AddressInfo> ValidAddresses = new List<AddressInfo>();
    [SerializeField] AddressInfo _address;
    [SerializeField] bool _isValidAddress = true;
    public AddressInfo Address => _address;
    public static void ClearValidAddresses()
    {
        ValidAddresses.Clear();
    }
    private void Awake()
    {
        if (_isValidAddress)
        {
            ValidAddresses.Add(_address);
        }
    }
    public bool MatchesAddress(AddressInfo address)
    {
        return _address.streetName == address.streetName && _address.number == address.number;
    }
    public void SetAddress(AddressInfo address)
    {
        _address = address;
    }
}