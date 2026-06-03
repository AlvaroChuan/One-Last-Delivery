using UnityEngine;

public class TrafficVehicleVisual : MonoBehaviour
{
    private uint _id;
    private TrafficGraph _graph;
    private int _targetEdgeIndex;
    private float _targetDistance;
    private float _currentDistance;
    private int _currentEdgeIndex;

    public void Initialize(TrafficGraph graph)
    {
        _graph = graph;
    }

    public void UpdateTarget(int edgeIndex, float distance)
    {
        if (_currentEdgeIndex != edgeIndex)
        {
            _currentEdgeIndex = edgeIndex;
            _currentDistance = distance; 
        }
        
        _targetEdgeIndex = edgeIndex;
        _targetDistance = distance;
    }

    private void Update()
    {
        if (_graph == null || _graph.edges.Count == 0) return;
        _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, Time.deltaTime * 10f);
        UpdateTransform(_currentEdgeIndex, _currentDistance);
    }

    private void UpdateTransform(int edgeIndex, float distance)
    {
        TrafficEdge edge = _graph.edges[edgeIndex];
        if (edge.points == null || edge.points.Length < 2) return;

        float normalizedT = Mathf.Clamp01(distance / edge.length);
        float floatIndex = normalizedT * (edge.points.Length - 1);
        int indexA = Mathf.FloorToInt(floatIndex);
        int indexB = Mathf.Min(indexA + 1, edge.points.Length - 1);
        float t = floatIndex - indexA;

        Vector3 targetPos = Vector3.Lerp(edge.points[indexA].position, edge.points[indexB].position, t);
        Vector3 targetDir = Vector3.Lerp(edge.points[indexA].tangent, edge.points[indexB].tangent, t);

        if (Vector3.Distance(transform.position, targetPos) > 5f)
        {
            transform.position = targetPos;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 15f);
        }

        if (targetDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 15f);
        }
    }
}
