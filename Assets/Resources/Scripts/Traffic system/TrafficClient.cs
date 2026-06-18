using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class TrafficClient : MonoBehaviour
{
    [SerializeField] private TrafficGraph _trafficGraph;
    [SerializeField] private GameObject[] _vehiclePrefabs;

    private Dictionary<uint, TrafficVehicleVisual> _vehicles = new Dictionary<uint, TrafficVehicleVisual>();
    [HideInInspector]
    public TrafficLightController[] trafficLights;

    private void Awake()
    {
        NetworkClient.RegisterHandler<TrafficBatchMessage>(OnTrafficBatchReceived);
        NetworkClient.RegisterHandler<ClientCarCrashCarMessage>(OnClientCarCrashCarReceived);
    }

    private void OnDestroy()
    {
        NetworkClient.UnregisterHandler<TrafficBatchMessage>();
        NetworkClient.UnregisterHandler<ClientCarCrashCarMessage>();
    }

    private Dictionary<uint, float> _vehicleLastUpdate = new Dictionary<uint, float>();

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
            if (!_vehicles.TryGetValue(state.id, out TrafficVehicleVisual visual))
            {
                visual = SpawnVehicle(state.id);
            }
            visual.UpdateTarget(state.currentEdgeId, state.distance, state.speed);
            _vehicleLastUpdate[state.id] = Time.time;
        }
    }

/*
    private void Update()
    {
        List<uint> toRemove = new List<uint>();
        foreach (var kvp in _vehicleLastUpdate)
        {
            if (Time.time - kvp.Value > 1.0f) // If not updated by the server in 1 second, it was despawned
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (uint id in toRemove)
        {
            if (_vehicles.TryGetValue(id, out TrafficVehicleVisual visual))
            {
                if (visual != null) Destroy(visual.gameObject);
                _vehicles.Remove(id);
            }
            _vehicleLastUpdate.Remove(id);
        }
    }
*/

    private TrafficVehicleVisual SpawnVehicle(uint id)
    {
        GameObject go = Instantiate(_vehiclePrefabs[id % _vehiclePrefabs.Length], transform);
        TrafficVehicleVisual visual = go.GetComponent<TrafficVehicleVisual>();
        visual.CarId = id;
        visual.Initialize(_trafficGraph);

        _vehicles.Add(id, visual);
        return visual;
    }

    private void OnClientCarCrashCarReceived(ClientCarCrashCarMessage message)
    {
        if (_vehicles.TryGetValue(message.carId, out TrafficVehicleVisual visual))
        {
            if (visual != null) Destroy(visual.gameObject);
            _vehicles.Remove(message.carId);
        }
        _vehicleLastUpdate.Remove(message.carId);
    }
}