using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

// Assumes only one package can match the address of the door.
public class DoorPackageDetectionComponent : NetworkBehaviour
{
    float _storedValue = 0f;
    float _storedKnockValue = 0f;
    GameObject _storedPackage = null;

    public float StoredValue => _storedValue;
    public float StoredKnockValue => _storedKnockValue;
    public GameObject StoredPackage => _storedPackage;

    bool _canLoseValue = true;
    public bool CanLoseValue
    {
        get => _canLoseValue;
        set => _canLoseValue = value;
    }

    AddressComponent _addressComponent;
    void Awake()
    {
        _addressComponent = GetComponent<AddressComponent>();
        if(_addressComponent == null)
        {
            _addressComponent = GetComponentInParent<AddressComponent>();
        }
        if(_addressComponent == null)
        {
            Debug.LogError("Door is missing an AddressComponent, please add one to the door or its parents.");
        }
    }
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Collision detected between {gameObject.name} and {collision.gameObject.name}");
        if (!isServer) return;

        if (!collision.collider.CompareTag("Package")) return;

        if (collision.gameObject.GetComponent<AddressComponent>().MatchesAddress(_addressComponent.StreetName, _addressComponent.Number))
        {
            Debug.Log($"Package {collision.gameObject.name} matches the address of the door {gameObject.name}. Storing package value.");
            _storedPackage = collision.gameObject;
            _storedValue = _storedPackage.GetComponent<PackageValueComponent>().GetValueWithKnockPenalty();
            _storedKnockValue = _storedPackage.GetComponent<PackageValueComponent>().GetKnockPenalty();
            MoneyManager.Instance.ServerAddMoney(_storedValue);
        }
    }
    void OnCollisionExit(Collision collision)
    {
        if (!isServer) return;

        if (collision.collider.CompareTag("Package"))
        {
            if (collision.gameObject == _storedPackage)
            {
                Debug.Log($"Package {collision.gameObject.name} has left the door {gameObject.name}. Clearing stored package value.");
                _storedPackage = null;
                if(_canLoseValue)
                {
                    Debug.Log($"Package value of {_storedValue} has been lost due to the package leaving the door.");
                    _storedValue = 0f;
                    _storedKnockValue = 0f;
                    MoneyManager.Instance.ServerSubtractMoney(_storedValue);
                }
            }
        }
    }
}