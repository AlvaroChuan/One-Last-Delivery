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
    [System.Serializable]
    private struct SpawnPointInfo
    {
        public Vector3 origin;
        public Vector3 size;
    }
    [SerializeField] private EnemySpawnEntry[] _enemySpawnEntries;
    [SerializeField] private float _spawnInterval = 60f;
    [SerializeField] private int _enemiesPerSpawn = 5;
    [SerializeField] private SpawnPointInfo[] _spawnpoints = new SpawnPointInfo[0];
    [SerializeField] private float _spawnpointRadius = 100f;
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
                    bool isInsideBuilding;
                    do
                    {
                        SpawnPointInfo spawnPointInfo = _spawnpoints[Random.Range(0, _spawnpoints.Length)];
                        float randomX = Random.Range(-spawnPointInfo.size.x / 2f, spawnPointInfo.size.x / 2f);
                        float randomZ = Random.Range(-spawnPointInfo.size.z / 2f, spawnPointInfo.size.z / 2f);
                        spawnPosition = spawnPointInfo.origin + new Vector3(randomX, 0f, randomZ);
                        isInNavMesh = NavMesh.SamplePosition(spawnPosition, out hit, 5f, NavMesh.AllAreas);
                        tooCloseToPlayer = false;
                        isInsideBuilding = Physics.Raycast(spawnPosition + Vector3.up, Vector3.up, out _, 100f);
                        foreach (var player in PlayerRegistry.SpawnedPlayers)
                        {
                            if (Vector3.Distance(player.transform.position, hit.position) < _minDistanceFromPlayer)
                            {
                                tooCloseToPlayer = true;
                                break;
                            }
                        }
                    } while (!isInNavMesh || tooCloseToPlayer || isInsideBuilding);

                    GameObject enemyInstance = Instantiate(entry.enemyPrefab, hit.position, Quaternion.identity);
                    NetworkServer.Spawn(enemyInstance);
                    break;
                }
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        foreach (var spawnPoint in _spawnpoints)
        {
            Gizmos.DrawWireCube(spawnPoint.origin, spawnPoint.size);
        }
    }
#endif
}