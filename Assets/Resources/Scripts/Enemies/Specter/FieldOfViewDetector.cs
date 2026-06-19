using UnityEngine;

public class FieldOfViewDetector : MonoBehaviour
{
    [SerializeField] LayerMask _obstructionMask;
    [SerializeField] Vector2 _padding = new Vector2(0.1f, 0.1f); // Padding to avoid edge cases
    [SerializeField] Transform[] _raycastOrigins; // Points from which to cast rays for FOV detection
    private bool _cachedIsInFOV;
    private bool _isCached;

    public bool IsInFOV(float maxDistance)
    {
        if (!_isCached)
        {
            _cachedIsInFOV = CalculateIsInFOV(maxDistance);
            _isCached = true;
        }
        return _cachedIsInFOV;
    }

    private bool CalculateIsInFOV(float maxDistance)
    {
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

    void LateUpdate()
    {
        _isCached = false;
    }
}
