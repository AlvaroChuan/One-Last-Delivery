using UnityEngine;

public class TrafficVehicleVisual : MonoBehaviour
{
    private TrafficGraph _graph;
    private int _targetEdgeIndex = -1;
    private float _logicalDistance;
    private float _networkSpeed;
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    
    [Header("Network Smoothing settings")]
    [SerializeField] private float _logicalCorrectionFactor = 0.1f;
    [SerializeField] private float _logicalDistanceThreshold = 10f;
    [SerializeField] private float _teleportDistanceThreshold = 15f;
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("Visual Catch-Up Settings")]
    [SerializeField] private float _catchUpMultiplier = 2.0f;
    [SerializeField] private float _overshootBrakeFactor = 0.3f;

    [Header("Lane Change Settings")]
    [SerializeField] private float _laneChangeSmoothTime = 1.0f;
    private float _currentLateralOffset = 0f;
    private float _lateralVelocity = 0f;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = false;

    public void Initialize(TrafficGraph graph) => _graph = graph;

    public void UpdateTarget(int edgeIndex, float distance, float speed)
    {
        if (_targetEdgeIndex != edgeIndex)
        {
            // If changing to a completely new edge, check if it's a parallel lane jump
            if (_targetEdgeIndex != -1 && _graph != null && _graph.edges.Count > edgeIndex)
            {
                TrafficEdge newEdge = _graph.edges[edgeIndex];
                float normalizedT = newEdge.length > 0 ? Mathf.Clamp01(distance / newEdge.length) : 0f;
                float floatIndex = normalizedT * (newEdge.points.Length - 1);
                int indexA = Mathf.FloorToInt(floatIndex);
                int indexB = Mathf.Min(indexA + 1, newEdge.points.Length - 1);
                float t = floatIndex - indexA;
                
                Vector3 newTargetPos = Vector3.Lerp(newEdge.points[indexA].position, newEdge.points[indexB].position, t);
                Vector3 newTargetDir = Vector3.Lerp(newEdge.points[indexA].tangent, newEdge.points[indexB].tangent, t);
                
                Vector3 toNewTarget = transform.position - newTargetPos;
                Vector3 rightVec = Vector3.Cross(Vector3.up, newTargetDir).normalized;
                float lateralDist = Vector3.Dot(toNewTarget, rightVec);
                
                // If it's a sideways jump (lane change), store the offset to smooth it out
                if (Mathf.Abs(lateralDist) > 1.5f && Mathf.Abs(lateralDist) < 10.0f) 
                {
                    _currentLateralOffset = lateralDist;
                }
            }

            _targetEdgeIndex = edgeIndex;
            _logicalDistance = distance;
        }
        else
        {
            float error = distance - _logicalDistance;
            if (Mathf.Abs(error) > _logicalDistanceThreshold)  _logicalDistance = distance;  
            else _logicalDistance += error * _logicalCorrectionFactor;  
        }
        _networkSpeed = speed;
    }

    private void Update()
    {
        if (_graph == null || _graph.edges.Count == 0 || _targetEdgeIndex < 0 || _targetEdgeIndex >= _graph.edges.Count) return;

        _logicalDistance += _networkSpeed * Time.deltaTime;

        TrafficEdge edge = _graph.edges[_targetEdgeIndex];
        
        // Clamp position to edge limits
        float clampedDistance = Mathf.Clamp(_logicalDistance, 0f, edge.length);
        float normalizedT = edge.length > 0 ? clampedDistance / edge.length : 0f;
        
        float floatIndex = normalizedT * (edge.points.Length - 1);
        int indexA = Mathf.FloorToInt(floatIndex);
        int indexB = Mathf.Min(indexA + 1, edge.points.Length - 1);
        float t = floatIndex - indexA;

        _targetPosition = Vector3.Lerp(edge.points[indexA].position, edge.points[indexB].position, t);
        Vector3 dir = Vector3.Lerp(edge.points[indexA].tangent, edge.points[indexB].tangent, t);
        
        if (dir != Vector3.zero) _targetRotation = Quaternion.LookRotation(dir);

        // Extrapolate when awaiting data and edge length is exceeded
        if (_logicalDistance > edge.length)
        {
            float excessDistance = _logicalDistance - edge.length;
            _targetPosition += _targetRotation * Vector3.forward * excessDistance;
        }

        // Apply Smooth Lateral Offset for Lane Changes
        if (Mathf.Abs(_currentLateralOffset) > 0.01f)
        {
            _currentLateralOffset = Mathf.SmoothDamp(_currentLateralOffset, 0f, ref _lateralVelocity, _laneChangeSmoothTime);
            Vector3 rightVec = Vector3.Cross(Vector3.up, dir).normalized;
            _targetPosition += rightVec * _currentLateralOffset;
        }

        Vector3 directionToTarget = _targetPosition - transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        Vector3 previousPosition = transform.position;

        // Debug variables
        float actualSpeed = 0f;
        string stateMsg = "";

        if (distanceToTarget > _teleportDistanceThreshold)
        {
            transform.position = _targetPosition;
            transform.rotation = _targetRotation;
            stateMsg = "TELEPORT (Critial desync)";
            actualSpeed = 0f;
        }
        else if (distanceToTarget > 0.05f)
        {
            directionToTarget /= distanceToTarget;
            float forwardDot = Vector3.Dot(transform.forward, directionToTarget);
            if (forwardDot < 0f)
            {
                actualSpeed = _networkSpeed * _overshootBrakeFactor;
                transform.position += _targetRotation * Vector3.forward * (actualSpeed * Time.deltaTime);
                stateMsg = "OVERSHOOT (Beaking to catch target)";
            }
            else
            {
                actualSpeed = _networkSpeed + (distanceToTarget * _catchUpMultiplier);
                transform.position = Vector3.MoveTowards(transform.position, _targetPosition, actualSpeed * Time.deltaTime);
                stateMsg = "CATCH-UP (Accelerating to catch target)";
            }

            // If changing lanes, point the car in the actual movement direction
            if (Mathf.Abs(_currentLateralOffset) > 0.1f)
            {
                Vector3 moveDir = (transform.position - previousPosition).normalized;
                if (moveDir.sqrMagnitude > 0.01f)
                {
                    _targetRotation = Quaternion.LookRotation(moveDir);
                }
            }
        }
        else
        {
            stateMsg = "SYNCED (Perectly in sync with target)";
            actualSpeed = _networkSpeed;
        }

        // Apply rotation scaled by physical distance traveled (physically accurate steering)
        if (distanceToTarget <= _teleportDistanceThreshold)
        {
            float distanceTravelled = actualSpeed * Time.deltaTime;
            // _rotationSpeed now acts as a steering multiplier per meter traveled
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, distanceTravelled * _rotationSpeed);
        }

        if (_showDebugLogs)
        {
            Debug.Log($"[Coche ID: {gameObject.GetInstanceID()}] Estado: <b>{stateMsg}</b> | Vel. Visual: {actualSpeed:F2} | Vel. Red: {_networkSpeed:F2} | Dist. Error: {distanceToTarget:F3}");
        }
    }
}