using UnityEngine;

public class FieldOfViewDetector : MonoBehaviour
{
    [SerializeField] float _maxViewDistance = 50f;
    [SerializeField] LayerMask _obstructionMask;
    [SerializeField] Vector2 _padding = new Vector2(0.1f, 0.1f); // Padding to avoid edge cases
    private bool _cachedIsInFOV;
    private bool _isCached;
    public bool IsInFOV()
    {
        if (!_isCached)
        {
            _cachedIsInFOV = CalculateIsInFOV();
            _isCached = true;
        }
        return _cachedIsInFOV;
    }

    private bool CalculateIsInFOV()
    {
        if (Vector3.Distance(transform.position, Camera.main.transform.position) > _maxViewDistance)
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

        Vector3 directionToCamera = (Camera.main.transform.position - transform.position).normalized;
        float distanceToCamera = Vector3.Distance(transform.position, Camera.main.transform.position);

        return !Physics.Raycast(transform.position, directionToCamera, distanceToCamera, _obstructionMask);
    }

    void LateUpdate()
    {
        _isCached = false;
    }
}
