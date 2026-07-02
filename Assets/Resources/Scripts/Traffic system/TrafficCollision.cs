using UnityEngine;
using Mirror;

[RequireComponent(typeof(TrafficVehicleVisual))]
public class TrafficCollision : MonoBehaviour
{
    public float crashVelocityThreshold = 5f;
    public uint CarId { get; set; }
    public float NetworkSpeed { get; set; }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if hit by a player
        if (collision.collider.CompareTag("WreckedCar") || collision.collider.CompareTag("Truck") || collision.collider.CompareTag("Player"))
        {
            NetworkIdentity ni = collision.collider.GetComponentInParent<NetworkIdentity>();

            // Only the client controlling the player should send the crash message to prevent duplicates
            if (ni != null && ni.isOwned)
            {
                Vector3 carVelocity = NetworkSpeed * transform.forward;
                Vector3 relativeVelocity = collision.relativeVelocity - carVelocity;
                float relativeVelocityMagnitude = relativeVelocity.magnitude;

                DevLogger.Log($"Collision detected with player. Relative velocity: {relativeVelocityMagnitude}");

                if (relativeVelocityMagnitude > crashVelocityThreshold)
                {
                    // Send crash message to the server
                    NetworkClient.Send(new ServerCarCrashCarMessage
                    {
                        carId = CarId,
                        position = transform.position,
                        rotation = transform.rotation,
                        impactVelocity = relativeVelocity
                    }, Channels.Reliable);
                }
            }
        }
    }
}
