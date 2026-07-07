using Mirror;
using UnityEngine;

public class SpeedCamera : NetworkBehaviour
{
    [SerializeField] private float _speedLimit = 10f; // Speed limit in units per second
    [SerializeField] private float _baseFineAmount = 5f; // Fine amount for speeding
    [SerializeField] private float _finePer5OverLimit = 2f; // Additional fine per 5 units over the speed limit
    [SerializeField] private float _fineCooldownTime = 5f; // Cooldown time in seconds to prevent multiple fines for the same truck
    [SerializeField] private Material _offMaterial; // Material to indicate the camera is off
    [SerializeField] private int _indicatorMaterialIndex = 1; // Index of the material to change when the camera is off
    [SerializeField] private float _enabledChance = 0.5f; // Chance for the speed camera to be enabled at startup
    [SerializeField] private AudioEvent _detectedAudioEvent; // Audio event to play when a truck is speeding
    [SyncVar(hook = nameof(OnEnabledChanged))] private bool _isEnabled = true;

    bool _isOnCooldown = false;
    float _cooldownTimer = 0f;
    private float _mpsToMph = 2.23694f; // Conversion factor from meters per second to miles per hour
    Light _light;

    void Awake()
    {
        SunManager.OnNightfall += DisableSpeedCamera;
        _light = GetComponentInChildren<Light>();
    }

    public override void OnStartServer()
    {
        _isEnabled = Random.value < _enabledChance;
    }

    public override void OnStartClient()
    {
        if (!_isEnabled)
        {
            DisableSpeedCamera();
        }
    }

    void OnDestroy()
    {
        SunManager.OnNightfall -= DisableSpeedCamera;
    }

    private void DisableSpeedCamera()
    {
        DevLogger.Log("Disabling speed camera.");
        MeshRenderer meshRenderer = GetComponentInChildren<MeshRenderer>();
        Material[] materials = meshRenderer.materials;
        if (_offMaterial != null && _indicatorMaterialIndex >= 0 && _indicatorMaterialIndex < materials.Length)
        {
            materials[_indicatorMaterialIndex] = _offMaterial;
            meshRenderer.materials = materials;
        }
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

    void OnEnabledChanged(bool oldValue, bool newValue)
    {
        if (!newValue)
        {
            DisableSpeedCamera();
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
        RpcDetectionEffects();
    }

    [ClientRpc]
    void RpcDetectionEffects()
    {
        _light.enabled = true;
        _detectedAudioEvent.Play(gameObject); // Play the detection audio event
        Invoke(nameof(TurnOffLight), 0.1f); // Turn off the light
    }

    void TurnOffLight()
    {
        _light.enabled = false;
    }
}
