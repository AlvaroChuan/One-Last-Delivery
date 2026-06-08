using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TrafficClient : MonoBehaviour
{
    [SerializeField] private TrafficGraph _trafficGraph;
    [SerializeField] private GameObject _vehiclePrefab;
    
    private Dictionary<uint, TrafficVehicleVisual> _vehicles = new Dictionary<uint, TrafficVehicleVisual>();

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