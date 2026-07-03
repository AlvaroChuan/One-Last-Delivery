using Mirror;
using UnityEngine;

public class SpeedChecker : MonoBehaviour
{
    [SerializeField] private float _speedLimit = 10f; // Speed limit in units per second
    [SerializeField] private float _baseFineAmount = 10f; // Fine amount for speeding
    [SerializeField] private float _finePerUnitOverLimit = 10f; // Additional fine per unit over the speed limit
    [SerializeField] private float _speedCheckInterval = 1f; // Interval for checking speed in seconds
    Rigidbody _truck;
    bool _alreadyFined = false;
    float _speedCheckTimer = 0f;
    int _truckEnteredCount = 0;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Truck"))
        {
            _truck = other.GetComponent<Rigidbody>();
            _truckEnteredCount++;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Truck"))
        {
            _truckEnteredCount--;
            if (_truckEnteredCount <= 0)
            {
                _truck = null;
                _alreadyFined = false; // Reset fine status when the truck exits
                _truckEnteredCount = 0; // Ensure count doesn't go negative
            }
        }
    }

    void Update()
    {
        if (_alreadyFined || _truck == null || !_truck.GetComponent<NetworkIdentity>().isOwned)
        {
            return; // Only process the truck if it has authority
        }

        _speedCheckTimer -= Time.deltaTime;
        if (_speedCheckTimer <= 0f)
        {
            CheckSpeed();
            _speedCheckTimer = _speedCheckInterval; // Reset the timer
        }
    }

    void CheckSpeed()
    {
        if (_alreadyFined || _truck == null || !_truck.GetComponent<NetworkIdentity>().isOwned)
        {
            return; // Only process the truck if it has authority
        }

        float truckSpeed = _truck.linearVelocity.magnitude;
        if (truckSpeed > _speedLimit)
        {
            float speedOverLimit = truckSpeed - _speedLimit;
            float fineAmount = _baseFineAmount + (speedOverLimit * _finePerUnitOverLimit);
            BalanceManager.RegisterTransaction("Speeding Fine", -fineAmount);
            _alreadyFined = true;
        }
    }
}
