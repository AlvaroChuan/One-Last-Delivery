using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using Unity.Collections;
using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Mathematics;

public class TrafficClient : MonoBehaviour
{
    private struct VisualVehicle
    {
        public GameObject gameObject;
        public TrafficCollision collision;
    }

    [SerializeField] private TrafficGraph _trafficGraph;
    [SerializeField] private GameObject[] _vehiclePrefabs;

    [Header("Network Smoothing settings")]
    [SerializeField] private float _logicalCorrectionFactor = 0.1f;
    [SerializeField] private float _logicalDistanceThreshold = 10f;
    [SerializeField] private float _teleportDistanceThreshold = 15f;
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("Visual Catch-Up Settings")]
    [SerializeField] private float _catchUpMultiplier = 2.0f;
    [SerializeField] private float _overshootBrakeFactor = 0.3f;

    [Header("Lane Change Settings")]
    [SerializeField] private float _laneChangeSmoothTime = 1.0f;

    [HideInInspector]
    public TrafficLightController[] trafficLights;

    // Burst arrays
    private NativeArray<NativeVisualNode> _nodes;
    private NativeArray<NativeVisualEdge> _edges;
    private NativeList<VehicleVisualState> _states;
    private TransformAccessArray _transformArray;

    private Dictionary<uint, int> _idToIndexMap = new Dictionary<uint, int>();
    private List<uint> _indexToIdMap = new List<uint>();

    private Dictionary<uint, VisualVehicle> _vehicles = new Dictionary<uint, VisualVehicle>();
    private Dictionary<uint, float> _vehicleLastUpdate = new Dictionary<uint, float>();

    private void Awake()
    {
        TrafficFader.OnCarsFadedOut += StopTrafficAndRemoveAllCars;
        NetworkClient.RegisterHandler<TrafficBatchMessage>(OnTrafficBatchReceived);
        NetworkClient.RegisterHandler<ClientCarCrashCarMessage>(OnClientCarCrashCarReceived);

        // Dynamically find and sort lights to prevent Unity scene-reference loss
        var allSceneLights = FindObjectsByType<TrafficLightController>(FindObjectsSortMode.None);
        List<TrafficLightController> validLights = new List<TrafficLightController>();
        foreach (var l in allSceneLights) if (l.lightId != -1) validLights.Add(l);
        validLights.Sort((a, b) => a.lightId.CompareTo(b.lightId));
        trafficLights = validLights.ToArray();

        InitializeGraphNativeArrays();

        _states = new NativeList<VehicleVisualState>(1000, Allocator.Persistent);
        _transformArray = new TransformAccessArray(1000);
    }

    private void InitializeGraphNativeArrays()
    {
        if (_trafficGraph == null || _trafficGraph.edges == null) return;

        int totalNodes = 0;
        foreach (var edge in _trafficGraph.edges) totalNodes += edge.points.Length;

        _nodes = new NativeArray<NativeVisualNode>(totalNodes, Allocator.Persistent);
        _edges = new NativeArray<NativeVisualEdge>(_trafficGraph.edges.Count, Allocator.Persistent);

        int nodeIndex = 0;
        for (int i = 0; i < _trafficGraph.edges.Count; i++)
        {
            var edge = _trafficGraph.edges[i];
            _edges[i] = new NativeVisualEdge
            {
                startIndex = nodeIndex,
                pointCount = edge.points.Length,
                length = edge.length
            };

            for (int j = 0; j < edge.points.Length; j++)
            {
                _nodes[nodeIndex++] = new NativeVisualNode
                {
                    position = edge.points[j].position,
                    tangent = edge.points[j].tangent
                };
            }
        }
    }

    private void OnDestroy()
    {
        NetworkClient.UnregisterHandler<TrafficBatchMessage>();
        NetworkClient.UnregisterHandler<ClientCarCrashCarMessage>();

        if (_nodes.IsCreated) _nodes.Dispose();
        if (_edges.IsCreated) _edges.Dispose();
        if (_states.IsCreated) _states.Dispose();
        if (_transformArray.isCreated) _transformArray.Dispose();

        TrafficFader.OnCarsFadedOut -= StopTrafficAndRemoveAllCars;
    }

    private void OnTrafficBatchReceived(TrafficBatchMessage message)
    {
        if (message.lightStates != null && message.lightStates.Length == trafficLights.Length)
        {
            for (int i = 0; i < message.lightStates.Length; i++)
            {
                if (trafficLights[i] != null)
                {
                    trafficLights[i].SetState((TrafficLightController.TrafficLightState)message.lightStates[i]);
                }
            }
        }

        foreach (NetworkVehicleState state in message.vehicles)
        {
            if (!_idToIndexMap.TryGetValue(state.id, out int index))
            {
                index = SpawnVehicle(state.id);
            }

            VehicleVisualState visualState = _states[index];

            // If changing to a completely new edge, check if it's a parallel lane jump
            if (visualState.targetEdgeIndex != state.currentEdgeId)
            {
                if (visualState.targetEdgeIndex != -1)
                {
                    NativeVisualEdge newEdge = _edges[state.currentEdgeId];
                    float normalizedT = newEdge.length > 0 ? Mathf.Clamp01(state.distance / newEdge.length) : 0f;
                    float floatIndex = normalizedT * (newEdge.pointCount - 1);
                    int indexA = Mathf.FloorToInt(floatIndex);
                    int indexB = Mathf.Min(indexA + 1, newEdge.pointCount - 1);
                    float t = floatIndex - indexA;

                    Vector3 newTargetPos = Vector3.Lerp(_nodes[newEdge.startIndex + indexA].position, _nodes[newEdge.startIndex + indexB].position, t);
                    Vector3 newTargetDir = Vector3.Lerp(_nodes[newEdge.startIndex + indexA].tangent, _nodes[newEdge.startIndex + indexB].tangent, t);

                    Vector3 toNewTarget = _vehicles[state.id].gameObject.transform.position - newTargetPos;
                    Vector3 rightVec = Vector3.Cross(Vector3.up, newTargetDir).normalized;
                    float lateralDist = Vector3.Dot(toNewTarget, rightVec);

                    if (Mathf.Abs(lateralDist) > 1.5f && Mathf.Abs(lateralDist) < 10.0f)
                    {
                        visualState.currentLateralOffset = lateralDist;
                    }
                }

                visualState.targetEdgeIndex = state.currentEdgeId;
                visualState.logicalDistance = state.distance;
            }
            else
            {
                float error = state.distance - visualState.logicalDistance;
                if (Mathf.Abs(error) > _logicalDistanceThreshold) visualState.logicalDistance = state.distance;
                else visualState.logicalDistance += error * _logicalCorrectionFactor;
            }

            visualState.networkSpeed = state.speed;
            _states[index] = visualState;

            _vehicleLastUpdate[state.id] = Time.time;
        }
    }

    private void Update()
    {
        if (!_states.IsCreated || _states.Length == 0) return;

        //Multithreaded Job Execution
        UpdateVehiclesVisualJob job = new UpdateVehiclesVisualJob
        {
            edges = _edges,
            nodes = _nodes,
            states = _states.AsArray(),
            deltaTime = Time.deltaTime,
            laneChangeSmoothTime = _laneChangeSmoothTime,
            teleportDistanceThreshold = _teleportDistanceThreshold,
            overshootBrakeFactor = _overshootBrakeFactor,
            catchUpMultiplier = _catchUpMultiplier,
            rotationSpeed = _rotationSpeed
        };

        JobHandle handle = job.Schedule(_transformArray);
        handle.Complete();

        foreach (var visualVehicle in _vehicles)
        {
            visualVehicle.Value.collision.NetworkSpeed = _states[_idToIndexMap[visualVehicle.Key]].networkSpeed;
        }
    }

    private int SpawnVehicle(uint id)
    {
        GameObject go = Instantiate(_vehiclePrefabs[id % _vehiclePrefabs.Length], transform);
        TrafficCollision collision = go.GetComponent<TrafficCollision>();
        collision.CarId = id;
        _vehicles.Add(id, new VisualVehicle { gameObject = go, collision = collision });

        int index = _states.Length;
        _idToIndexMap[id] = index;
        _indexToIdMap.Add(id);

        _states.Add(new VehicleVisualState { targetEdgeIndex = -1 });
        _transformArray.Add(go.transform);

        return index;
    }

    private void DespawnVehicle(uint id)
    {
        if (_idToIndexMap.TryGetValue(id, out int index))
        {
            Debug.Log($"Despawning vehicle with ID: {id} at index: {index}");
            VisualVehicle visualVehicle = _vehicles[id];
            Destroy(visualVehicle.gameObject);

            _vehicles.Remove(id);
            _vehicleLastUpdate.Remove(id);

            int lastIndex = _states.Length - 1;

            _transformArray.RemoveAtSwapBack(index);

            // If the element removed was not the last one, update the swapped element's index map
            if (index != lastIndex)
            {
                VehicleVisualState lastState = _states[lastIndex];
                _states[index] = lastState;

                uint swappedId = _indexToIdMap[lastIndex];
                _idToIndexMap[swappedId] = index;
                _indexToIdMap[index] = swappedId;
            }

            _states.RemoveAtSwapBack(lastIndex);
            _indexToIdMap.RemoveAt(lastIndex);
            _idToIndexMap.Remove(id);
        }
    }

    public void StopTrafficAndRemoveAllCars()
    {
        foreach (var vehicle in _vehicles)
        {
            Destroy(vehicle.Value.gameObject);
        }

        _vehicles.Clear();
        _vehicleLastUpdate.Clear();
        _idToIndexMap.Clear();
        _indexToIdMap.Clear();
        _states.Clear();
        NetworkClient.UnregisterHandler<TrafficBatchMessage>();
        NetworkClient.UnregisterHandler<ClientCarCrashCarMessage>();
        enabled = false;
    }

    private void OnClientCarCrashCarReceived(ClientCarCrashCarMessage message)
    {
        DespawnVehicle(message.carId);
    }
}