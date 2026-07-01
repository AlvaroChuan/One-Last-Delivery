using Unity.Collections;
using UnityEngine;
using Mirror;
using Unity.Jobs;
using System.Collections.Generic;

public class TrafficManager : NetworkBehaviour
{
    [Header("Simulation Settings")]
    [SerializeField] private TrafficGraph _trafficGraph;
    [SerializeField] private GameObject _wreckPrefab;
    [SerializeField] private int _initialVehiclesToSpawn = 200;
    [SerializeField] private int _maxVehicleCapacity = 2000;
    [SerializeField] private float _vehicleMaxSpeed = 20f;
    [SerializeField] private float _vehicleAcceleration = 5f;
    [SerializeField] private float _safeDistance = 5f;
    [SerializeField] private float _spawnSpacing = 15f;
    [SerializeField] private int _respawnEdgeId = 0; // Default edge ID for respawning vehicles

    [Header("Network Settings")]
    [SerializeField] private float _networkTickRate = 0.1f;
    [SerializeField] private int _maxBatchSize = 100;

    [Header("Traffic Light Settings")]
    [SerializeField] private float _greenTime = 10f;
    [SerializeField] private float _yellowTime = 3f;
    [SerializeField] private float _bothRedTime = 1f;

    [Header("Dynamic Obstacles")]
    [SerializeField] private float _spatialGridCellSize = 20f;
    [SerializeField] private float _obstacleSnapDistance = 5f;

    // Edges and Vehicles
    private NativeArray<NativeVehicle> _previousVehicleStates;
    private NativeArray<NativeVehicle> _vehicleStates;
    private NativeArray<NativeEdge> _edgeStates;
    private NativeParallelMultiHashMap<int, int> _edgeToVehiclesMap;
    private NativeArray<ushort> _edgeConnections;
    private NativeArray<ushort> _edgeConflicts;
    private NativeArray<int> _nodeLocks;
    private NativeArray<byte> _edgeStopSignals;

    // Traffic Light Data
    private NativeArray<NativeIntersection> _intersections;
    private NativeArray<int> _intersectionLightIds;
    private NativeArray<ushort> _lightToEdgeMapping;
    private NativeArray<byte> _lightStates;

    // Spatial Grid for Dynamic Obstacles
    private NativeArray<EdgePoint> _allPoints;
    private NativeParallelMultiHashMap<int, int> _spatialGrid;
    private NativeArray<DynamicObstacleData> _dynamicObstacles;
    private NativeArray<NativeObstacle> _mappedObstacles;

    [HideInInspector]
    public TrafficLightController[] trafficLights;
    private float _nextSyncTime;

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<ServerCarCrashCarMessage>(OnCrashCarMessage);

        InitializeGraph();
        InitializeVehicles();
    }

    public override void OnStopServer()
    {
        NetworkServer.UnregisterHandler<ServerCarCrashCarMessage>();
    }

    private void Update()
    {
        if (!isServer || !_vehicleStates.IsCreated) return;

        // Zero out edge stop signals (Job will write to them)
        for (int i = 0; i < _edgeStopSignals.Length; i++) _edgeStopSignals[i] = 0;

        _previousVehicleStates.CopyFrom(_vehicleStates);
        _edgeToVehiclesMap.Clear();

        TrafficLightSimulationJob lightJob = new TrafficLightSimulationJob
        {
            intersections = _intersections,
            intersectionLightIds = _intersectionLightIds,
            lightToEdgeMapping = _lightToEdgeMapping,
            lightStates = _lightStates,
            edgeStopSignals = _edgeStopSignals,
            deltaTime = Time.deltaTime,
            greenTime = _greenTime,
            yellowTime = _yellowTime,
            bothRedTime = _bothRedTime
        };
        JobHandle lightHandle = lightJob.Schedule(_intersections.Length, 8);

        PopulateMapJob populateJob = new PopulateMapJob
        {
            vehicles = _previousVehicleStates,
            edgeMap = _edgeToVehiclesMap.AsParallelWriter()
        };
        JobHandle populateHandle = populateJob.Schedule(_vehicleStates.Length, 64);

        ClearLocksJob clearLocksJob = new ClearLocksJob
        {
            locks = _nodeLocks
        };
        JobHandle clearLocksHandle = clearLocksJob.Schedule(_nodeLocks.Length, 64, populateHandle);

        // Update Dynamic Obstacles
        if (TrafficObstacle.ActiveObstacles.Count > _dynamicObstacles.Length)
        {
            // Resize Native Arrays if we have more obstacles than capacity
            int newCapacity = Mathf.Max(_dynamicObstacles.Length * 2, TrafficObstacle.ActiveObstacles.Count);
            _dynamicObstacles.Dispose();
            _mappedObstacles.Dispose();
            _dynamicObstacles = new NativeArray<DynamicObstacleData>(newCapacity, Allocator.Persistent);
            _mappedObstacles = new NativeArray<NativeObstacle>(newCapacity, Allocator.Persistent);
        }

        // Fill NativeArray with active obstacles
        int activeCount = 0;
        foreach (var obs in TrafficObstacle.ActiveObstacles)
        {
            if (obs != null)
            {
                _dynamicObstacles[activeCount] = new DynamicObstacleData
                {
                    id = (uint)activeCount,
                    position = obs.transform.position
                };
                activeCount++;
            }
        }

        MapObstaclesJob mapObstaclesJob = new MapObstaclesJob
        {
            inputObstacles = _dynamicObstacles,
            spatialGrid = _spatialGrid,
            edges = _edgeStates,
            allPoints = _allPoints,
            cellSize = _spatialGridCellSize,
            snapDistance = _obstacleSnapDistance,
            outputObstacles = _mappedObstacles
        };
        // We only schedule the Map job for the actual number of active obstacles!
        JobHandle mapObstaclesHandle = mapObstaclesJob.Schedule(activeCount, 64, clearLocksHandle);

        CarSimulationJob simulationJob = new CarSimulationJob
        {
            vehicles = _vehicleStates,
            previousStates = _previousVehicleStates,
            vehicleMap = _edgeToVehiclesMap,
            edges = _edgeStates,
            connections = _edgeConnections,
            conflicts = _edgeConflicts,
            nodeLocks = _nodeLocks,
            edgeStopSignals = _edgeStopSignals,
            dynamicObstacles = _mappedObstacles,
            deltaTime = Time.deltaTime,
            maxSpeed = _vehicleMaxSpeed,
            acceleration = _vehicleAcceleration,
            safeDistance = _safeDistance,
            randomSeed = (uint) Random.Range(1, 100000)
        };

        JobHandle combinedHandle = JobHandle.CombineDependencies(mapObstaclesHandle, lightHandle);
        JobHandle handle = simulationJob.Schedule(_vehicleStates.Length, 64, combinedHandle);
        handle.Complete();

        if (Time.time >= _nextSyncTime)
        {
            _nextSyncTime = Time.time + _networkTickRate;
            BroadcastTrafficStates();
        }
    }

    private void BroadcastTrafficStates()
    {
        byte[] currentLightStates = _lightStates.ToArray();

        List<NetworkVehicleState> activeVehicles = new List<NetworkVehicleState>();
        for (int i = 0; i < _vehicleStates.Length; i++)
        {
            if (_vehicleStates[i].currentEdgeId != -1)
            {
                activeVehicles.Add(new NetworkVehicleState
                {
                    id = _vehicleStates[i].id,
                    currentEdgeId = _vehicleStates[i].currentEdgeId,
                    distance = _vehicleStates[i].distance,
                    speed = _vehicleStates[i].speed
                });
            }
        }

        int batches = Mathf.CeilToInt((float)activeVehicles.Count / _maxBatchSize);
        for (int b = 0; b < batches; b++)
        {
            int offset = b * _maxBatchSize;
            int count = Mathf.Min(_maxBatchSize, activeVehicles.Count - offset);
            NetworkVehicleState[] batch = new NetworkVehicleState[count];
            activeVehicles.CopyTo(offset, batch, 0, count);

            NetworkServer.SendToReady(new TrafficBatchMessage { vehicles = batch, lightStates = currentLightStates }, Channels.Unreliable);
        }
    }

    public void SpawnVehicle(int edgeId, float distance)
    {
        if (!_vehicleStates.IsCreated) return;
        for (int i = 0; i < _vehicleStates.Length; i++)
        {
            if (_vehicleStates[i].currentEdgeId == -1)
            {
                var v = _vehicleStates[i];
                v.currentEdgeId = edgeId;
                v.distance = distance;
                v.speed = 0f;
                _vehicleStates[i] = v;
                return;
            }
        }
        Debug.LogWarning("Traffic Vehicle Pool is full!");
    }

    public void DespawnVehicle(uint vehicleId)
    {
        if (!_vehicleStates.IsCreated) return;
        for (int i = 0; i < _vehicleStates.Length; i++)
        {
            if (_vehicleStates[i].id == vehicleId)
            {
                var v = _vehicleStates[i];
                v.currentEdgeId = -1;
                _vehicleStates[i] = v;

                NetworkServer.SendToReady(new ClientCarCrashCarMessage { carId = vehicleId }, Channels.Reliable);
                return;
            }
        }
    }

    private void OnCrashCarMessage(NetworkConnectionToClient conn, ServerCarCrashCarMessage msg)
    {
        // 1. Despawn from Job System
        DespawnVehicle(msg.carId);

        // 2. Spawn the physical wreck
        if (_wreckPrefab != null)
        {
            GameObject wreck = Instantiate(_wreckPrefab, msg.position, msg.rotation);
            NetworkServer.Spawn(wreck);

            Rigidbody rb = wreck.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Give it a rough tumbling force based on player impact
                rb.AddForce(msg.impactVelocity, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * msg.impactVelocity.magnitude, ForceMode.Impulse);
            }
        }

        // 3. Respawn a replacement vehicle
        SpawnVehicle(_respawnEdgeId, 0f); // Respawn at the start of the designated edge
    }

    private void InitializeGraph()
    {
        // Dynamically find and sort lights to prevent Unity scene-reference loss
        var allSceneLights = FindObjectsByType<TrafficLightController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<TrafficLightController> validLights = new List<TrafficLightController>();
        foreach (var l in allSceneLights) 
        {
            if (l.lightId != -1) validLights.Add(l);
        }
        validLights.Sort((a, b) => a.lightId.CompareTo(b.lightId));
        trafficLights = validLights.ToArray();
        
        Debug.Log($"[TrafficManager] Found {allSceneLights.Length} lights in scene. {validLights.Count} have valid lightIds.");

        _edgeStates = new NativeArray<NativeEdge>(_trafficGraph.edges.Count, Allocator.Persistent);

        List<ushort> allConnections = new List<ushort>();
        List<ushort> allConflicts = new List<ushort>();
        List<EdgePoint> allPointsList = new List<EdgePoint>();

        // Count total points for spatial hash estimation
        int totalPoints = 0;
        foreach (var e in _trafficGraph.edges) totalPoints += e.points.Length;

        _spatialGrid = new NativeParallelMultiHashMap<int, int>(totalPoints, Allocator.Persistent);

        for (int i = 0; i < _trafficGraph.edges.Count; i++)
        {
            int startIndex = allConnections.Count;
            int count = _trafficGraph.edges[i].nextEdgeIDs != null ? _trafficGraph.edges[i].nextEdgeIDs.Length : 0;
            if (count > 0) allConnections.AddRange(_trafficGraph.edges[i].nextEdgeIDs);

            int conflictStart = allConflicts.Count;
            int conflictCount = _trafficGraph.edges[i].conflictingEdgeIDs != null ? _trafficGraph.edges[i].conflictingEdgeIDs.Length : 0;
            if (conflictCount > 0) allConflicts.AddRange(_trafficGraph.edges[i].conflictingEdgeIDs);

            int pointsStart = allPointsList.Count;
            int pointsCount = _trafficGraph.edges[i].points.Length;

            HashSet<int> edgeGridCells = new HashSet<int>();

            for (int p = 0; p < pointsCount; p++)
            {
                EdgePoint pt = _trafficGraph.edges[i].points[p];
                allPointsList.Add(pt);

                int gridX = (int)Mathf.Floor(pt.position.x / _spatialGridCellSize);
                int gridZ = (int)Mathf.Floor(pt.position.z / _spatialGridCellSize);
                int gridKey = (gridX * 73856) ^ (gridZ * 19284);

                // Add to multi-hash map if not already added for this edge
                if (edgeGridCells.Add(gridKey))
                {
                    _spatialGrid.Add(gridKey, i);
                }
            }

            _edgeStates[i] = new NativeEdge
            {
                id = _trafficGraph.edges[i].id,
                length = _trafficGraph.edges[i].length,
                connectionStartIndex = startIndex,
                connectionCount = count,
                conflictStartIndex = conflictStart,
                conflictCount = conflictCount,
                pointsStartIndex = pointsStart,
                pointsCount = pointsCount,
                leftEdgeId = GetEdgeIndexByID(_trafficGraph.edges[i].leftEdgeId),
                rightEdgeId = GetEdgeIndexByID(_trafficGraph.edges[i].rightEdgeId)
            };
        }

        _edgeConnections = new NativeArray<ushort>(allConnections.ToArray(), Allocator.Persistent);
        _edgeConflicts = new NativeArray<ushort>(allConflicts.ToArray(), Allocator.Persistent);
        _allPoints = new NativeArray<EdgePoint>(allPointsList.ToArray(), Allocator.Persistent);
        _nodeLocks = new NativeArray<int>(_trafficGraph.edges.Count, Allocator.Persistent);
        _edgeStopSignals = new NativeArray<byte>(_trafficGraph.edges.Count, Allocator.Persistent);

        // Initialize max 10 dynamic obstacles
        _dynamicObstacles = new NativeArray<DynamicObstacleData>(10, Allocator.Persistent);
        _mappedObstacles = new NativeArray<NativeObstacle>(10, Allocator.Persistent);

        _intersections = new NativeArray<NativeIntersection>(_trafficGraph.intersections.Count, Allocator.Persistent);
        List<int> allLightIds = new List<int>();
        for (int i = 0; i < _trafficGraph.intersections.Count; i++)
        {
            var data = _trafficGraph.intersections[i];
            int aStart = allLightIds.Count;
            if (data.phaseALightIds != null) allLightIds.AddRange(data.phaseALightIds);
            int bStart = allLightIds.Count;
            if (data.phaseBLightIds != null) allLightIds.AddRange(data.phaseBLightIds);

            _intersections[i] = new NativeIntersection
            {
                phaseAStartIndex = aStart,
                phaseACount = data.phaseALightIds != null ? data.phaseALightIds.Length : 0,
                phaseBStartIndex = bStart,
                phaseBCount = data.phaseBLightIds != null ? data.phaseBLightIds.Length : 0,
                currentTimer = _greenTime,
                currentStep = 0
            };
        }
        _intersectionLightIds = new NativeArray<int>(allLightIds.ToArray(), Allocator.Persistent);

        _lightToEdgeMapping = new NativeArray<ushort>(trafficLights.Length, Allocator.Persistent);
        for (int i = 0; i < trafficLights.Length; i++)
        {
            _lightToEdgeMapping[i] = trafficLights[i] != null ? trafficLights[i].edgeId : (ushort)0xFFFF;
        }

        _lightStates = new NativeArray<byte>(trafficLights.Length, Allocator.Persistent);
    }

    private void InitializeVehicles()
    {
        List<NativeVehicle> possibleSpawns = new List<NativeVehicle>();

        for (int i = 0; i < _edgeStates.Length; i++)
        {
            float edgeLength = _edgeStates[i].length;
            int maxCarsOnEdge = Mathf.FloorToInt(edgeLength / _spawnSpacing);

            for (int j = 0; j < maxCarsOnEdge; j++)
            {
                possibleSpawns.Add(new NativeVehicle
                {
                    currentEdgeId = i,
                    distance = j * _spawnSpacing,
                    speed = 0f
                });
            }
        }

        for (int i = 0; i < possibleSpawns.Count; i++)
        {
            NativeVehicle temp = possibleSpawns[i];
            int randomIndex = Random.Range(i, possibleSpawns.Count);
            possibleSpawns[i] = possibleSpawns[randomIndex];
            possibleSpawns[randomIndex] = temp;
        }

        int carsToSpawn = Mathf.Min(_initialVehiclesToSpawn, possibleSpawns.Count);

        _vehicleStates = new NativeArray<NativeVehicle>(_maxVehicleCapacity, Allocator.Persistent);
        _previousVehicleStates = new NativeArray<NativeVehicle>(_maxVehicleCapacity, Allocator.Persistent);
        _edgeToVehiclesMap = new NativeParallelMultiHashMap<int, int>(_maxVehicleCapacity, Allocator.Persistent);

        // Pre-allocate the entire pool as inactive
        for (int i = 0; i < _maxVehicleCapacity; i++)
        {
            _vehicleStates[i] = new NativeVehicle
            {
                id = (uint)i,
                currentEdgeId = -1, // -1 means inactive
                distance = 0f,
                speed = 0f
            };
        }

        // Activate the initial vehicles
        for (int i = 0; i < carsToSpawn; i++)
        {
            NativeVehicle v = possibleSpawns[i];
            v.id = (uint)i;
            _vehicleStates[i] = v;
        }

        Debug.Log($"Initialized Pool: {_maxVehicleCapacity} capacity. Spawned: {carsToSpawn} initially.");
    }

    private int GetEdgeIndexByID(int id)
    {
        if (id < 0) return -1;
        for (int i = 0; i < _trafficGraph.edges.Count; i++)
        {
            if (_trafficGraph.edges[i].id == id) return i;
        }
        return -1;
    }

    private void OnDestroy()
    {
        if (isServer) NetworkServer.UnregisterHandler<ServerCarCrashCarMessage>();

        if (_vehicleStates.IsCreated) _vehicleStates.Dispose();
        if (_edgeStates.IsCreated) _edgeStates.Dispose();
        if (_edgeConnections.IsCreated) _edgeConnections.Dispose();
        if (_edgeConflicts.IsCreated) _edgeConflicts.Dispose();
        if (_nodeLocks.IsCreated) _nodeLocks.Dispose();
        if (_edgeStopSignals.IsCreated) _edgeStopSignals.Dispose();
        if (_previousVehicleStates.IsCreated) _previousVehicleStates.Dispose();
        if (_edgeToVehiclesMap.IsCreated) _edgeToVehiclesMap.Dispose();
        if (_intersections.IsCreated) _intersections.Dispose();
        if (_intersectionLightIds.IsCreated) _intersectionLightIds.Dispose();
        if (_lightToEdgeMapping.IsCreated) _lightToEdgeMapping.Dispose();
        if (_lightStates.IsCreated) _lightStates.Dispose();
        if (_allPoints.IsCreated) _allPoints.Dispose();
        if (_spatialGrid.IsCreated) _spatialGrid.Dispose();
        if (_dynamicObstacles.IsCreated) _dynamicObstacles.Dispose();
        if (_mappedObstacles.IsCreated) _mappedObstacles.Dispose();
    }
}