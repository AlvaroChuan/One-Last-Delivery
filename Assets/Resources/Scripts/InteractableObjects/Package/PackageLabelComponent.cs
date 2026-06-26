using TMPro;
using UnityEngine;

[RequireComponent(typeof(NetworkAddressComponent))]
public class PackageLabelComponent : MonoBehaviour
{
    [SerializeField] TextMeshPro _streetLabel;
    [SerializeField] TextMeshPro _numberLabel;
    NetworkAddressComponent _addressComponent;
    void Awake()
    {
        _addressComponent = GetComponent<NetworkAddressComponent>();
    }
    void OnEnable()
    {
        _addressComponent.onAddressChanged += UpdateLabel;
    }
    void OnDisable()
    {
        _addressComponent.onAddressChanged -= UpdateLabel;
    }

    void UpdateLabel(AddressInfo oldAddress, AddressInfo newAddress)
    {
        _streetLabel.text = newAddress.streetName;
        _numberLabel.text = newAddress.number.ToString();
    }
}
