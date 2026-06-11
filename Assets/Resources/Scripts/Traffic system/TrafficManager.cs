using Unity.Collections;
using UnityEngine;
using Mirror;
using Unity.Jobs;
using System.Collections.Generic;

public class TrafficManager : NetworkBehaviour
{
    [Header("Traffic Settings")]
    [SerializeField] private TrafficGraph _trafficGraph;
    [SerializeField] private int _totalVehicles = 100;
    [SerializeField] private float _vehicleMaxSpeed = 20f; // Could add variability here
    [SerializeField] private float _vehicleAcceleration = 5f; // Could add variability here
    [SerializeField] private float _safeDistance = 5f;
    [SerializeField] private float _spawnSpacing = 15f;

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
    
    public TrafficLightController[] trafficLights;
    private float _nextSyncTime;

    private void Start()
    {
        if (!isServer) return; 
        InitializeGraph();
        InitializeVehicles();
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

        int batches = Mathf.CeilToInt((float)_totalVehicles / _maxBatchSize);
        for (int b = 0; b < batches; b++)
        {
            int offset = b * _maxBatchSize;
            int count = Mathf.Min(_maxBatchSize, _totalVehicles - offset);
            NetworkVehicleState[] batch = new NetworkVehicleState[count];

            for (int i = 0; i < count; i++)
            {
                int index = offset + i;
                batch[i] = new NetworkVehicleState
                {
                    id = _vehicleStates[index].id,
                    currentEdgeId = _vehicleStates[index].currentEdgeId,
                    distance = _vehicleStates[index].distance,
                    speed = _vehicleStates[index].speed
                };
            }

            NetworkServer.SendToReady(new TrafficBatchMessage { vehicles = batch, lightStates = currentLightStates }, Channels.Unreliable);
        }
    }

    private void InitializeGraph()
    {
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

        int carsToSpawn = Mathf.Min(_totalVehicles, possibleSpawns.Count);
        
        _vehicleStates = new NativeArray<NativeVehicle>(carsToSpawn, Allocator.Persistent);
        _previousVehicleStates = new NativeArray<NativeVehicle>(carsToSpawn, Allocator.Persistent);
        _edgeToVehiclesMap = new NativeParallelMultiHashMap<int, int>(carsToSpawn, Allocator.Persistent);

        for (int i = 0; i < carsToSpawn; i++)
        {
            NativeVehicle v = possibleSpawns[i];
            v.id = (uint)i;
            _vehicleStates[i] = v;
        }

        Debug.Log($"Spawned: {carsToSpawn} cars of {_totalVehicles} requested.");
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