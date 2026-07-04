using Mirror;
using UnityEngine;

public class SpeedCamera : NetworkBehaviour
{
    [SerializeField] private float _speedLimit = 10f; // Speed limit in units per second
    [SerializeField] private float _baseFineAmount = 5f; // Fine amount for speeding
    [SerializeField] private float _finePer5OverLimit = 2f; // Additional fine per 5 units over the speed limit
    [SerializeField] private float _fineCooldownTime = 5f; // Cooldown time in seconds to prevent multiple fines for the same truck

    bool _isOnCooldown = false;
    float _cooldownTimer = 0f;
    private float _mpsToMph = 2.23694f; // Conversion factor from meters per second to miles per hour

    void Awake()
    {
        SunManager.OnNightfall += DisableSpeedCamera;
    }

    private void DisableSpeedCamera()
    {
        GetComponent<Collider>().enabled = false;
    }

    void Update()
    {
        if (_isOnCooldown)
        {
            _cooldownTimer += Time.deltaTime;
            if (_cooldownTimer >= _fineCooldownTime)
            {
                _isOnCooldown = false;
                _cooldownTimer = 0f;
            }
        }
    }

    public void SetSpeedLimit(float speedLimit)
    {
        _speedLimit = speedLimit;
    }

    void OnTriggerExit(Collider other)
    {
        if (_isOnCooldown) return;
        _isOnCooldown = true;

        Rigidbody rb = other.attachedRigidbody;
        Vector3 truckVelocity = rb.linearVelocity;
        truckVelocity.y = 0; // Ignore vertical speed
        float truckSpeed = truckVelocity.magnitude * _mpsToMph; // Convert to mph
        if (truckSpeed > _speedLimit)
        {
            Vector3 vecProduct = Vector3.Cross(truckVelocity.normalized, transform.forward);
            DevLogger.Log($"Cross Product Y: {vecProduct.y}");
            if (vecProduct.y < 0)
            {
                float speedOverLimit = truckSpeed - _speedLimit;
                float fineAmount = _baseFineAmount + Mathf.Floor(speedOverLimit / 5f) * _finePer5OverLimit;
                string reason = $"Speeding ticket";
                DevLogger.Log($"Truck {other.name} is speeding! Speed: {truckSpeed:F2}, Fine: {fineAmount}");
                CmdRegisterFine(reason, -fineAmount);
            }
        }
    }

    [Command(requiresAuthority = false)]
    void CmdRegisterFine(string reason, float amount)
    {
        BalanceManager.RegisterTransaction(reason, amount);
    }
}
