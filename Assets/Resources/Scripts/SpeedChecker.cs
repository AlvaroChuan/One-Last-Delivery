using Mirror;
using UnityEngine;

public class SpeedChecker : NetworkBehaviour
{
    [SerializeField] private float _speedLimit = 10f; // Speed limit in units per second
    [SerializeField] private float _baseFineAmount = 10f; // Fine amount for speeding
    [SerializeField] private float _finePerTenOverLimit = 10f; // Additional fine per 10 units over the speed limit
    [SerializeField] private float _speedCheckInterval = 1f; // Interval for checking speed in seconds
    Rigidbody _truck;
    bool _alreadyFined = false;
    float _speedCheckTimer = 0f;
    int _truckEnteredCount = 0;

    void OnTriggerEnter(Collider other)
    {
        DevLogger.Log($"Truck entered speed zone: {other.name}");
        if(_truckEnteredCount == 0)
        {
            _truck = other.GetComponent<Rigidbody>();
            _speedCheckTimer = 0;
            _alreadyFined = false; // Reset fine status when a new truck enters
        }
        _truckEnteredCount++;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Truck") || other.CompareTag("Player"))
        {
            _truckEnteredCount--;
            if (_truckEnteredCount <= 0)
            {
                _truck = null;
                _alreadyFined = false; // Reset fine status when the truck exits
                _truckEnteredCount = 0; // Ensure count doesn't go negative
                _speedCheckTimer = 0; // Reset the timer when the truck exits
            }
        }
    }

    void FixedUpdate()
    {
        if (_alreadyFined || _truck == null || !_truck.GetComponent<NetworkIdentity>().isOwned)
        {
            return; // Only process the truck if it has authority
        }

        _speedCheckTimer -= Time.fixedDeltaTime;
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
        DevLogger.Log($"Truck speed: {truckSpeed}, Speed limit: {_speedLimit}");
        if (truckSpeed > _speedLimit)
        {
            float speedOverLimit = truckSpeed - _speedLimit;
            float fineAmount = _baseFineAmount + (speedOverLimit * _finePerTenOverLimit / 10f);
            CmdRegisterFine("Speeding Fine", -fineAmount);
            _alreadyFined = true;
        }
    }

    [Command(requiresAuthority = false)]
    void CmdRegisterFine(string reason, float amount)
    {
        BalanceManager.RegisterTransaction(reason, amount);
    }
}
