using Mirror;
using UnityEngine;

public class SpeedCamera : MonoBehaviour
{
    [SerializeField] private float _speedLimit = 10f; // Speed limit in units per second
    [SerializeField] private float _baseFineAmount = 10f; // Fine amount for speeding
    [SerializeField] private float _finePerUnitOverLimit = 10f; // Additional fine per unit over the speed limit
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Truck"))
        {
            if (!other.GetComponent<NetworkIdentity>().isOwned)
            {
                return; // Only process the truck if it has authority
            }
            Rigidbody truckRigidbody = other.GetComponent<Rigidbody>();
            if (truckRigidbody != null)
            {
                float truckSpeed = truckRigidbody.linearVelocity.magnitude;
                if (truckSpeed > _speedLimit)
                {
                    float speedOverLimit = truckSpeed - _speedLimit;
                    float fineAmount = _baseFineAmount + (speedOverLimit * _finePerUnitOverLimit);
                    BalanceManager.RegisterTransaction("Speeding Fine", -fineAmount);
                }
            }
        }
    }
}
