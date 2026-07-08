using Mirror;
using UnityEngine;

public class FieldOfViewDetector : MonoBehaviour
{
    [SerializeField] LayerMask _obstructionMask;
    [SerializeField] Vector2 _padding = new Vector2(0.1f, 0.1f); // Padding to avoid edge cases
    [SerializeField] Transform[] _raycastOrigins; // Points from which to cast rays for FOV detection
    private bool _cachedIsInFOV;
    private bool _isCached;
    float _cachedDetectionRange = 0f;

    public bool IsInFOV(float maxDistance)
    {
        if (!_isCached || _cachedDetectionRange != maxDistance)
        {
            _cachedIsInFOV = CalculateIsInFOV(maxDistance);
            _isCached = true;
            _cachedDetectionRange = maxDistance;
        }
        return _cachedIsInFOV;
    }

    private bool CalculateIsInFOV(float maxDistance)
    {
        if (PlayerDead())
        {
            return false; // If the player is dead, consider them out of FOV
        }

        if (Vector3.Distance(transform.position, Camera.main.transform.position) > maxDistance)
        {
            return false;
        }
        Vector3 cameraPosition = Camera.main.WorldToViewportPoint(transform.position);

        if (cameraPosition.x < 0f - _padding.x || cameraPosition.x > 1f + _padding.x ||
            cameraPosition.y < 0f - _padding.y || cameraPosition.y > 1f + _padding.y ||
            cameraPosition.z < 0f)
        {
            return false;
        }

        Vector3 camPos = Camera.main.transform.position;

        foreach (Transform origin in _raycastOrigins)
        {
            Vector3 directionToPoint = (origin.position - camPos).normalized;
            float distanceToPoint = Vector3.Distance(camPos, origin.position);

            if (!Physics.Raycast(camPos, directionToPoint, distanceToPoint, _obstructionMask, QueryTriggerInteraction.Ignore))
            {
                return true; // Found a clear sliver of sight! The object is NOT completely blocked.
            }
        }

        return false;
    }


    bool PlayerDead()
    {
        if(NetworkClient.connection == null || NetworkClient.connection.identity == null)
        {
            return true; // Consider player dead if we can't find the player object
        }
        GameObject player = NetworkClient.connection.identity?.gameObject;
        if (player == null)
        {
            DevLogger.LogError("Player object is null. Ensure the player is connected and has a valid NetworkIdentity.");
            return true; // Consider player dead if we can't find the player object
        }

        PlayerDeathComponent playerDeathComponent = player.GetComponent<PlayerDeathComponent>();
        if (playerDeathComponent == null)
        {
            DevLogger.LogError("PlayerDeathComponent is missing on the player object.");
            return true; // Consider player dead if the component is missing
        }

        return playerDeathComponent.IsDead;
    }

    void LateUpdate()
    {
        _isCached = false;
    }
}
