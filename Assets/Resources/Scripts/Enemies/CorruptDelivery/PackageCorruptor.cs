using UnityEngine;
using Mirror;

[RequireComponent(typeof(PackageSpawner))]
public class PackageCorruptor : NetworkBehaviour
{
    [SerializeField] float _corruptionInterval = 180f;
    [SerializeField] float _corruptionChance = 0.5f; // 50% chance to corrupt a package
    PackageSpawner _packageSpawner;
    bool _isNightTime = false;
    float _corruptionTimer = 0f;
    public override void OnStartServer()
    {
        _packageSpawner = GetComponent<PackageSpawner>();
        SunManager.OnNightfall += OnNightfall;
    }

    void OnDestroy()
    {
        SunManager.OnNightfall -= OnNightfall;
    }

    void OnNightfall()
    {
        if (isServer)
        {
            if (Random.value < _corruptionChance)
            {
                _packageSpawner.TryCorruptPackage();
            }
            _isNightTime = true;
        }
    }

    void Update()
    {
        if (!isServer || !_isNightTime)
            return;

        _corruptionTimer += Time.deltaTime;
        if (_corruptionTimer >= _corruptionInterval)
        {
            _corruptionTimer = 0f;
            if (Random.value < _corruptionChance)
            {
                _packageSpawner.TryCorruptPackage();
            }
        }
    }
}