using UnityEngine;
using Mirror;

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
        if (collision.collider.CompareTag("Player"))
        {
            NetworkIdentity ni = collision.collider.GetComponentInParent<NetworkIdentity>();
            
            // Only the client controlling the player should send the crash message to prevent duplicates
            if (ni != null && ni.isOwned)
            {
                if (collision.relativeVelocity.magnitude > crashVelocityThreshold)
                {
                    // Send crash message to the server
                    NetworkClient.Send(new CrashCarMessage 
                    { 
                        carId = _visual.CarId,
                        position = transform.position,
                        rotation = transform.rotation,
                        impactVelocity = collision.relativeVelocity
                    });
                }
            }
        }
    }
}
