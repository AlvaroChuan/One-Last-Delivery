using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class PackageSpawner : NetworkBehaviour
{
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
    [SerializeField] PackageEntry[] _packageEntries;
    [SerializeField] Vector3Int _spawnBounds;
    [SerializeField] int _packagesToSpawn = 5;
    [SerializeField] CoordinateOrder _coordinateOrder = CoordinateOrder.XZY;
    [SerializeField] float _packageSize = 1f;
    public static List<AddressInfo> UsedAddresses = new List<AddressInfo>();

    void Awake()
    {
        UsedAddresses.Clear();
        NormalizeProbabilities();
    }
    public override void OnStartServer()
    {
        SpawnPackages();
    }

    void SpawnPackages()
    {
        Vector3Int currentPosition = Vector3Int.zero;
        AddressLibrary addressLibrary = Resources.Load<AddressLibrary>(AddressLibrary.GetResourcePath());
        if (addressLibrary == null || addressLibrary.AddressCount == 0)
        {
            DevLogger.LogError("Address library is missing or empty. Please generate the address library before spawning packages.");
            return;
        }
        if (_packagesToSpawn > addressLibrary.AddressCount)
        {
            DevLogger.LogError("Not enough valid addresses to assign unique addresses to all packages. Only spawning " + addressLibrary.AddressCount + " packages.");
            _packagesToSpawn = addressLibrary.AddressCount; // Adjust the number of packages to spawn to match the number of available addresses
        }
        for (int i = 0; i < _packagesToSpawn; i++)
        {
            GameObject packagePrefab = GetRandomPackagePrefab();

            Vector3 spawnPosition = transform.position + new Vector3(currentPosition.x * _packageSize, currentPosition.y * _packageSize, currentPosition.z * _packageSize);
            GameObject packageInstance = Instantiate(packagePrefab, spawnPosition, Quaternion.identity);
            NetworkServer.Spawn(packageInstance);

            NetworkAddressComponent addressComponent = packageInstance.GetComponent<NetworkAddressComponent>();
            AddressInfo newAddress;
            do
            {
                newAddress = addressLibrary.GetRandomAddress();
            } while (UsedAddresses.Contains(newAddress));

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
}