using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TrafficGraphBaker : MonoBehaviour
{
    [SerializeField] TrafficGraph _outputGraph;
    [SerializeField] SplineContainer[] _splineContainers;
    [SerializeField] int _pointsPerEdge = 20;
    [SerializeField] float _connectionThreshold = 1.0f;

    [ContextMenu("Bake Splines to Graph")]
    public void BakeGraph()
    {
#if UNITY_EDITOR
        _outputGraph.edges.Clear();
        ushort currentEdgeID = 0;

        foreach (var container in _splineContainers)
        {
            foreach (var spline in container.Splines)
            {
                TrafficEdge newEdge = new TrafficEdge
                {
                    id = currentEdgeID,
                    length = spline.GetLength(),
                    points = new EdgePoint[_pointsPerEdge],
                    nextEdgeIDs = new ushort[0],
                    endNodeID = -1
                };

                for (int i = 0; i < _pointsPerEdge; i++)
                {
                    float t = i / (float)(_pointsPerEdge - 1);
                    spline.Evaluate(t, out var localPosition, out var localTangent, out _);

                    newEdge.points[i] = new EdgePoint
                    {
                        position = container.transform.TransformPoint(localPosition),
                        tangent = container.transform.TransformDirection(localTangent).normalized
                    };
                }

                _outputGraph.edges.Add(newEdge);
                currentEdgeID++;
            }
        }

        foreach (var edge in _outputGraph.edges)
        {
            Vector3 endPoint = edge.points[edge.points.Length - 1].position;
            List<ushort> connectedEdges = new List<ushort>();

            foreach (var potentialNextEdge in _outputGraph.edges)
            {
                if (edge.id == potentialNextEdge.id) continue;
                Vector3 startPoint = potentialNextEdge.points[0].position;
                if (Vector3.Distance(endPoint, startPoint) <= _connectionThreshold) connectedEdges.Add(potentialNextEdge.id);
            }

            edge.nextEdgeIDs = connectedEdges.ToArray();
        }

        EditorUtility.SetDirty(_outputGraph);
        AssetDatabase.SaveAssets();
        Debug.Log($"Bake completed: {_outputGraph.edges.Count} edges generated.");
#endif
    }
}
