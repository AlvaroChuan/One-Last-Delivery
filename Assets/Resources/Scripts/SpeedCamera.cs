using Mirror;
using UnityEngine;

public class SpeedCamera : NetworkBehaviour
{
    [SerializeField] private float _speedLimit = 10f; // Speed limit in units per second
    [SerializeField] private float _baseFineAmount = 10f; // Fine amount for speeding
    [SerializeField] private float _finePerTenOverLimit = 10f; // Additional fine per 10 units over the speed limit

    public void SetSpeedLimit(float speedLimit)
    {
        _speedLimit = speedLimit;
    }

    void OnTriggerEnter(Collider other)
    {
        DevLogger.Log($"Truck entered speed zone: {other.name}");

        Rigidbody rb = other.GetComponent<Rigidbody>();
        Vector3 truckVelocity = rb.linearVelocity;
        truckVelocity.y = 0; // Ignore vertical speed
        float truckSpeed = truckVelocity.magnitude;
        if (truckSpeed > _speedLimit)
        {
            Vector3 vecProduct = Vector3.Cross(other.transform.forward, transform.forward);
            if (vecProduct.y > 0)
            {
                float speedOverLimit = truckSpeed - _speedLimit;
                float fineAmount = _baseFineAmount + Mathf.Floor(speedOverLimit / 10f) * _finePerTenOverLimit;
                string reason = $"Speeding ticket";
                DevLogger.Log($"Truck {other.name} is speeding! Speed: {truckSpeed:F2}, Fine: {fineAmount}");
                CmdRegisterFine(reason, fineAmount);
            }
        }
    }

    [Command(requiresAuthority = false)]
    void CmdRegisterFine(string reason, float amount)
    {
        BalanceManager.RegisterTransaction(reason, amount);
    }
}
