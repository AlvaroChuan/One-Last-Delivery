using UnityEngine;

public class TrafficVehicleConfig : MonoBehaviour
{
    [Header("Simulation Settings")]
    public float maxSpeed = 20f;
    public float acceleration = 5f;
    public float safeDistance = 5f;

    [Header("Wreck Settings")]
    public GameObject wreckPrefab;
}
