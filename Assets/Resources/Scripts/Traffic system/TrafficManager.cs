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

    [Header("Network Settings")]
    [SerializeField] private float _networkTickRate = 0.1f;
    [SerializeField] private int _maxBatchSize = 100;

    private NativeArray<NativeVehicle> _previousVehicleStates;
    private NativeArray<NativeVehicle> _vehicleStates;
    private NativeArray<NativeEdge> _edgeStates;
    private NativeParallelMultiHashMap<int, int> _edgeToVehiclesMap;
    private NativeArray<ushort> _edgeConnections;
    private NativeArray<int> _nodeLocks;
    private int _totalNodes;
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

        _previousVehicleStates.CopyFrom(_vehicleStates);
        _edgeToVehiclesMap.Clear();

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

        TrafficSimulationJob simulationJob = new TrafficSimulationJob
        {
            vehicles = _vehicleStates,
            previousstates = _previousVehicleStates,
            vehicleMap = _edgeToVehiclesMap,
            edges = _edgeStates,
            connections = _edgeConnections,
            nodeLocks = _nodeLocks,
            deltaTime = Time.deltaTime,
            maxSpeed = _vehicleMaxSpeed,
            acceleration = _vehicleAcceleration,
            safeDistance = _safeDistance,
            randomSeed = (uint) Random.Range(1, 100000)
        };
        JobHandle handle = simulationJob.Schedule(_vehicleStates.Length, 64, populateHandle);
        handle.Complete();

        if (Time.time >= _nextSyncTime)
        {
            _nextSyncTime = Time.time + _networkTickRate;
            BroadcastTrafficStates();
        }
    }

    private void BroadcastTrafficStates()
    {
        int totalVehicles = _vehicleStates.Length;
        int totalBatches = Mathf.CeilToInt((float)totalVehicles / _maxBatchSize);

        for (int i = 0; i < totalBatches; i++)
        {
            int currentBatchSize = Mathf.Min(_maxBatchSize, totalVehicles - (i * _maxBatchSize));
            NetworkVehicleState[] batch = new NetworkVehicleState[currentBatchSize];

            for (int j = 0; j < currentBatchSize; j++)
            {
                int index = (i * _maxBatchSize) + j;
                batch[j] = new NetworkVehicleState
                {
                    id = _vehicleStates[index].id,
                    currentEdgeIndex = _vehicleStates[index].currentEdgeIndex,
                    distance = _vehicleStates[index].distance
                };
            }

            NetworkServer.SendToReady(new TrafficBatchMessage { vehicles = batch }, Channels.Unreliable);
        }
    }

    private void InitializeGraph()
    {
        _edgeStates = new NativeArray<NativeEdge>(_trafficGraph.edges.Count, Allocator.Persistent);
        
        List<ushort> allConnections = new List<ushort>();

        for (int i = 0; i < _trafficGraph.edges.Count; i++)
        {
            int startIndex = allConnections.Count;
            int count = _trafficGraph.edges[i].nextEdgeIDs.Length;
            allConnections.AddRange(_trafficGraph.edges[i].nextEdgeIDs);

            _edgeStates[i] = new NativeEdge
            {
                id = _trafficGraph.edges[i].id,
                length = _trafficGraph.edges[i].length,
                connectionStartIndex = startIndex,
                connectionCount = count,
                endNodeID = _trafficGraph.edges[i].endNodeID
            };
        }

        _edgeConnections = new NativeArray<ushort>(allConnections.ToArray(), Allocator.Persistent);
        _nodeLocks = new NativeArray<int>(_totalNodes, Allocator.Persistent);
    }

    private void InitializeVehicles()
    {
        _vehicleStates = new NativeArray<NativeVehicle>(_totalVehicles, Allocator.Persistent);
        _previousVehicleStates = new NativeArray<NativeVehicle>(_totalVehicles, Allocator.Persistent);
        _edgeToVehiclesMap = new NativeParallelMultiHashMap<int, int>(_totalVehicles, Allocator.Persistent);
        
        for (int i = 0; i < _totalVehicles; i++)
        {
            int randomEdge = Random.Range(0, _edgeStates.Length);
            
            _vehicleStates[i] = new NativeVehicle
            {
                id = (uint)i,
                currentEdgeIndex = randomEdge,
                distance = Random.Range(0f, _edgeStates[randomEdge].length),
                speed = 0f
            };
        }
    }

    private void OnDestroy()
    {
        if (_vehicleStates.IsCreated) _vehicleStates.Dispose();
        if (_edgeStates.IsCreated) _edgeStates.Dispose();
        if (_edgeConnections.IsCreated) _edgeConnections.Dispose();
        if (_previousVehicleStates.IsCreated) _previousVehicleStates.Dispose();
        if (_edgeToVehiclesMap.IsCreated) _edgeToVehiclesMap.Dispose();
        if (_nodeLocks.IsCreated) _nodeLocks.Dispose();
    }
}