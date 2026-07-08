using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class PackageSpawner : NetPersistentDataManager<PackageSpawner, PackageSpawner.PackageSpawnerStaticState, int>
{
    public class PackageSpawnerStaticState : StaticStateBase
    {
        public bool isFirstDay = true;
        public override void Reset()
        {
            StaticData = 0;
            isFirstDay = true;
        }
    }
    public enum CoordinateOrder
    {
        XYZ,
        XZY,
        YXZ,
        YZX,
        ZXY,
        ZYX
    }
    [System.Serializable]
    public class PackageEntry
    {
        public GameObject prefab;
        public float probability;
    }
    [Header("Settings")]
    [SerializeField] private int _initialPackagesToSpawn = 5;
    [SerializeField] private int _extraPackagesToSpawnPerDay = 1;
    [SerializeField] PackageEntry[] _packageEntries;
    [SerializeField] Vector3Int _spawnBounds;
    [SerializeField] CoordinateOrder _coordinateOrder = CoordinateOrder.XZY;
    [SerializeField] Vector3Int _spawnDirection = Vector3Int.one;
    [SerializeField] float _packageSize = 1f;
    [SerializeField] int _maxCorruptedPackages = 2; // Maximum number of packages that can be corrupted at once
    public static List<AddressInfo> UsedAddresses = new List<AddressInfo>();
    [SyncVar] private int _packagesToSpawn;
    private List<GameObject> _spawnedPackages = new List<GameObject>();
    public List<GameObject> SpawnedPackages => _spawnedPackages; // Public getter for spawned packages
    private AddressLibrary _addressLibrary;
    private List<bool> _corruptedPackages = new List<bool>(); // Track which packages are corrupted
    public List<bool> CorruptedPackages => _corruptedPackages; // Public getter for corrupted packages
    bool _allPackagesDeliveredNotified = false; // Flag to ensure the notification is sent only once

    protected override void Awake()
    {
        base.Awake();
        _addressLibrary = Resources.Load<AddressLibrary>(AddressLibrary.GetResourcePath());
        if (_addressLibrary == null)
        {
            DevLogger.LogError("Address library is missing. Please generate the address library before spawning packages.");
        }
    }

    void Update()
    {
        if (!isServer) return; // Only the server should handle package corruption

        if (FreePackageCount() <= 0 && !_allPackagesDeliveredNotified)
        {
            _allPackagesDeliveredNotified = true; // Set the flag to true to prevent further notifications
            DevLogger.Log("Delivered all packages.");
            (NetworkManager.singleton as CustomNetworkManager).NotifyAllPackagesDelivered();
        }
    }

    protected override void ServerInitializeStaticData()
    {
        if (StaticDataState.isFirstDay)
        {
            StaticDataState.StaticData = _initialPackagesToSpawn;
            StaticDataState.isFirstDay = false;
        }
        else
        {
            StaticDataState.StaticData += _extraPackagesToSpawnPerDay;
        }
        SpawnPackages();
    }

    protected override void ServerUpdateInstanceData()
    {
        _packagesToSpawn = StaticDataState.StaticData;
    }

    public void SpawnPackages()
    {
        DevLogger.Log("Spawning packages. Total to spawn: " + _packagesToSpawn);
        NormalizeProbabilities();
        UsedAddresses.Clear();
        Vector3Int currentPosition = Vector3Int.zero;

        if (_packagesToSpawn > _addressLibrary.AddressCount)
        {
            DevLogger.LogWarning("Not enough valid addresses to assign unique addresses to all packages. Only spawning " + _addressLibrary.AddressCount + " packages. Tried to spawn " + _packagesToSpawn + " packages.");
            _packagesToSpawn = _addressLibrary.AddressCount; // Adjust the number of packages to spawn to match the number of available addresses
        }

        for (int i = 0; i < _packagesToSpawn; i++)
        {
            GameObject packagePrefab = GetRandomPackagePrefab();

            Vector3 spawnPosition = transform.position + new Vector3(currentPosition.x * _packageSize * _spawnDirection.x, currentPosition.y * _packageSize * _spawnDirection.y, currentPosition.z * _packageSize * _spawnDirection.z);
            GameObject packageInstance = Instantiate(packagePrefab, spawnPosition, Quaternion.identity);
            NetworkServer.Spawn(packageInstance);
            _spawnedPackages.Add(packageInstance);
            _corruptedPackages.Add(false); // Initially, no packages are corrupted

            NetworkAddressComponent addressComponent = packageInstance.GetComponent<NetworkAddressComponent>();

            AddressInfo newAddress = GetUnusedAddress();

            if (newAddress.streetName == null || newAddress.streetName == "") // Check for empty or null street name
            {
                NetworkServer.Destroy(packageInstance); // Destroy the package instance to avoid having a package without a valid address
                DevLogger.LogError("Failed to find a valid unused address for package spawning. This likely means there are not enough unique addresses available. Stopping spawning process to prevent infinite loop.");
                break; // Exit the loop to prevent further spawning
            }

            addressComponent.SetAddress(newAddress);
            UsedAddresses.Add(newAddress);

            UpdateSpawnPosition(ref currentPosition);
        }
    }

    void NormalizeProbabilities()
    {
        float total = 0f;
        foreach (var entry in _packageEntries)
        {
            total += entry.probability;
        }
        if (total == 0f)
        {
            DevLogger.LogWarning("Total probability is zero. Assigning equal probabilities to all package entries.");
            for (int i = 0; i < _packageEntries.Length; i++)
            {
                _packageEntries[i].probability = 1f / _packageEntries.Length;
            }
            return;
        }
        for (int i = 0; i < _packageEntries.Length; i++)
        {
            _packageEntries[i].probability /= total;
        }
    }

    GameObject GetRandomPackagePrefab()
    {
        float totalProbability = 0f;
        foreach (var entry in _packageEntries)
        {
            totalProbability += entry.probability;
        }
        float randomValue = Random.Range(0f, totalProbability);
        float cumulativeProbability = 0f;
        foreach (var entry in _packageEntries)
        {
            cumulativeProbability += entry.probability;
            if (randomValue <= cumulativeProbability)
            {
                return entry.prefab;
            }
        }
        return _packageEntries[0].prefab; // Fallback
    }

    void UpdateSpawnPosition(ref Vector3Int currentPosition)
    {
        switch (_coordinateOrder)
        {
            case CoordinateOrder.XYZ:
                currentPosition.x++;
                if (currentPosition.x >= _spawnBounds.x)
                {
                    currentPosition.x = 0;
                    currentPosition.y++;
                    if (currentPosition.y >= _spawnBounds.y)
                    {
                        currentPosition.y = 0;
                        currentPosition.z++;
                    }
                }
                break;
            case CoordinateOrder.XZY:
                currentPosition.x++;
                if (currentPosition.x >= _spawnBounds.x)
                {
                    currentPosition.x = 0;
                    currentPosition.z++;
                    if (currentPosition.z >= _spawnBounds.z)
                    {
                        currentPosition.z = 0;
                        currentPosition.y++;
                    }
                }
                break;
            case CoordinateOrder.YXZ:
                currentPosition.y++;
                if (currentPosition.y >= _spawnBounds.y)
                {
                    currentPosition.y = 0;
                    currentPosition.x++;
                    if (currentPosition.x >= _spawnBounds.x)
                    {
                        currentPosition.x = 0;
                        currentPosition.z++;
                    }
                }
                break;
            case CoordinateOrder.YZX:
                currentPosition.y++;
                if (currentPosition.y >= _spawnBounds.y)
                {
                    currentPosition.y = 0;
                    currentPosition.z++;
                    if (currentPosition.z >= _spawnBounds.z)
                    {
                        currentPosition.z = 0;
                        currentPosition.x++;
                    }
                }
                break;
            case CoordinateOrder.ZXY:
                currentPosition.z++;
                if (currentPosition.z >= _spawnBounds.z)
                {
                    currentPosition.z = 0;
                    currentPosition.x++;
                    if (currentPosition.x >= _spawnBounds.x)
                    {
                        currentPosition.x = 0;
                        currentPosition.y++;
                    }
                }
                break;
            case CoordinateOrder.ZYX:
                currentPosition.z++;
                if (currentPosition.z >= _spawnBounds.z)
                {
                    currentPosition.z = 0;
                    currentPosition.y++;
                    if (currentPosition.y >= _spawnBounds.y)
                    {
                        currentPosition.y = 0;
                        currentPosition.x++;
                    }
                }
                break;
        }
    }

    public bool TryCorruptPackage()
    {
        if (!EnoughPackagesToCorrupt())
        {
            DevLogger.LogWarning("Not enough packages to corrupt. Skipping corruption process.");
            return false;
        }

        int corruptedPackage;
        do
        {
            corruptedPackage = Random.Range(0, _spawnedPackages.Count);
        } while (_spawnedPackages[corruptedPackage] == null || _corruptedPackages[corruptedPackage]);

        _corruptedPackages[corruptedPackage] = true; // Mark this package as corrupted

        NetworkAddressComponent addressComponent = _spawnedPackages[corruptedPackage].GetComponent<NetworkAddressComponent>();

        AddressInfo newAddress = GetUnusedAddress();

        if (newAddress.streetName == null || newAddress.streetName == "") // Check for empty or null street name
        {
            DevLogger.LogError("Failed to find a valid unused address for corruption. This likely means there are not enough unique addresses available. Stopping corruption process to prevent infinite loop.");
            return false; // Exit the method to prevent further corruption
        }

        addressComponent.SetAddress(newAddress);
        UsedAddresses.Add(newAddress);

        GameObject door = _addressLibrary.GetDoorForAddress(newAddress);
        DevLogger.Log($"Found door for corrupted package: {door}, at address: {newAddress}");
        door.GetComponent<DoorController>().CorruptDoor();

        DevLogger.Log($"Package at {_spawnedPackages[corruptedPackage].transform.position} has been corrupted with a new address: {newAddress}");
        return true;
    }

    bool EnoughPackagesToCorrupt()
    {
        int freePackages = FreePackageCount();
        if (freePackages <= _maxCorruptedPackages)
        {
            DevLogger.LogWarning($"Not enough free packages to corrupt. Free packages: {freePackages}, Max corrupted packages allowed: {_maxCorruptedPackages}");
            return false;
        }
        return true;
    }

    int FreePackageCount()
    {
        int count = 0;
        for (int i = 0; i < _spawnedPackages.Count; i++)
        {
            if (_spawnedPackages[i] != null && !_corruptedPackages[i])
            {
                count++;
            }
        }
        return count;
    }

    AddressInfo GetUnusedAddress()
    {
        AddressInfo newAddress;
        int attempts = 0;
        do
        {
            newAddress = _addressLibrary.GetRandomAddress();
            attempts++;
            if (attempts > 100)
            {
                DevLogger.LogError("Failed to find a unique address after 100 attempts. This likely means there are not enough unique addresses available. Stopping process to prevent infinite loop.");
                return new AddressInfo(); // Return an empty address info to avoid null reference exceptions
            }
        } while (UsedAddresses.Contains(newAddress));
        return newAddress;
    }
}