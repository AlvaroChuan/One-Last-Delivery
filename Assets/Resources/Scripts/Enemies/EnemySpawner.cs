using UnityEngine;
using Mirror;

public class EnemySpawner : NetworkBehaviour
{
    [System.Serializable]
    private struct EnemySpawnEntry
    {
        [SerializeField] public GameObject enemyPrefab;
        [SerializeField, Range(0, 1)] public float additiveSpawnChance;
    }
    [SerializeField] private EnemySpawnEntry[] _enemySpawnEntries;
    [SerializeField] private float _spawnInterval = 60f;
    [SerializeField] private int _enemiesPerSpawn = 5;
    bool _isNightTime = false;

    private float _spawnTimer = 0f;

    public override void OnStartServer()
    {
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
            _isNightTime = true;
            SpawnEnemies();
        }
    }

    void Update()
    {
        if (!isServer || !_isNightTime)
            return;

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;
            SpawnEnemies();
        }
    }

    [Server]
    void SpawnEnemies()
    {
        for (int i = 0; i < _enemiesPerSpawn; i++)
        {
            float totalChance = 0f;
            foreach (var entry in _enemySpawnEntries)
            {
                totalChance += entry.additiveSpawnChance;
            }

            float randomValue = Random.value * totalChance;
            float cumulativeChance = 0f;

            foreach (var entry in _enemySpawnEntries)
            {
                cumulativeChance += entry.additiveSpawnChance;
                if (randomValue <= cumulativeChance)
                {
                    GameObject enemyInstance = Instantiate(entry.enemyPrefab, transform.position, Quaternion.identity);
                    NetworkServer.Spawn(enemyInstance);
                    break;
                }
            }
        }
    }
}