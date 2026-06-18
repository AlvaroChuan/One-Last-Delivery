using UnityEngine;
using Mirror;

[RequireComponent(typeof(TrafficVehicleVisual))]
public class TrafficCollision : MonoBehaviour
{
    private TrafficVehicleVisual _visual;
    public float crashVelocityThreshold = 5f;

    private void Awake()
    {
        _visual = GetComponent<TrafficVehicleVisual>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if hit by a player
        if (collision.collider.CompareTag("WreckedCar") || collision.collider.CompareTag("Truck") || collision.collider.CompareTag("Player"))
        {
            NetworkIdentity ni = collision.collider.GetComponentInParent<NetworkIdentity>();

            // Only the client controlling the player should send the crash message to prevent duplicates
            if (ni != null && ni.isOwned)
            {
                Vector3 carVelocity = _visual.NetworkSpeed * transform.forward;
                Vector3 relativeVelocity = collision.relativeVelocity - carVelocity;
                float relativeVelocityMagnitude = relativeVelocity.magnitude;

                DevLogger.Log($"Collision detected with player. Relative velocity: {relativeVelocityMagnitude}");

                if (relativeVelocityMagnitude > crashVelocityThreshold)
                {
                    // Send crash message to the server
                    NetworkClient.Send(new ServerCarCrashCarMessage
                    {
                        carId = _visual.CarId,
                        position = transform.position,
                        rotation = transform.rotation,
                        impactVelocity = relativeVelocity
                    }, Channels.Reliable);
                }
            }
        }
    }
}
