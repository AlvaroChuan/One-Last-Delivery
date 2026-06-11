using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TrafficClient : MonoBehaviour
{
    [SerializeField] private TrafficGraph _trafficGraph;
    [SerializeField] private GameObject _vehiclePrefab;
    
    private Dictionary<uint, TrafficVehicleVisual> _vehicles = new Dictionary<uint, TrafficVehicleVisual>();
    [HideInInspector]
    public TrafficLightController[] trafficLights;

    private void Start()
    {
        NetworkClient.RegisterHandler<TrafficBatchMessage>(OnTrafficBatchReceived);
    }

    private void OnDestroy()
    {
        NetworkClient.UnregisterHandler<TrafficBatchMessage>();
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
            if (!_vehicles.TryGetValue(state.id, out TrafficVehicleVisual visual))
            {
                visual = SpawnVehicle(state.id);
            }
            visual.UpdateTarget(state.currentEdgeId, state.distance, state.speed);
        }
    }

    private TrafficVehicleVisual SpawnVehicle(uint id)
    {
        GameObject go = Instantiate(_vehiclePrefab);
        TrafficVehicleVisual visual = go.GetComponent<TrafficVehicleVisual>();
        visual.Initialize(_trafficGraph);

        _vehicles.Add(id, visual);
        return visual;
    }
}