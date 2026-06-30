using UnityEngine;
using Mirror;
using UnityEngine.AI;

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
    [SerializeField] private Vector2 _spawnAreaBound1 = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 _spawnAreaBound2 = new Vector2(10f, 10f);
    [SerializeField] private float _minDistanceFromPlayer = 20f;
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
                    NavMeshHit hit;
                    Vector3 spawnPosition;
                    bool isInNavMesh;
                    bool tooCloseToPlayer;
                    do
                    {
                        spawnPosition = new Vector3(Random.Range(_spawnAreaBound1.x, _spawnAreaBound2.x), 0, Random.Range(_spawnAreaBound1.y, _spawnAreaBound2.y));
                        isInNavMesh = NavMesh.SamplePosition(spawnPosition, out hit, 5f, NavMesh.AllAreas);
                        tooCloseToPlayer = false;
                        foreach (var player in PlayerRegistry.SpawnedPlayers)
                        {
                            if (Vector3.Distance(player.transform.position, hit.position) < _minDistanceFromPlayer)
                            {
                                tooCloseToPlayer = true;
                                break;
                            }
                        }
                    } while (!isInNavMesh || tooCloseToPlayer);

                    GameObject enemyInstance = Instantiate(entry.enemyPrefab, hit.position, Quaternion.identity);
                    NetworkServer.Spawn(enemyInstance);
                    break;
                }
            }
        }
    }
}