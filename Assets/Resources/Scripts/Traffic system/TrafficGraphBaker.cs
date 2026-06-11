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
    [SerializeField] float _trafficLightSearchRadius = 15.0f;

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
                if (Vector3.Distance(endPoint, startPoint) <= _connectionThreshold) 
                {
                    connectedEdges.Add(potentialNextEdge.id);
                }
            }
            edge.nextEdgeIDs = connectedEdges.ToArray();
        }

        foreach (var edge in _outputGraph.edges)
        {
            List<ushort> conflicts = new List<ushort>();
            foreach (var potentialNextEdge in _outputGraph.edges)
            {
                if (edge.id == potentialNextEdge.id) continue;

                // Do not consider direct connections as conflicts
                if (System.Array.IndexOf(edge.nextEdgeIDs, potentialNextEdge.id) >= 0) continue;
                if (System.Array.IndexOf(potentialNextEdge.nextEdgeIDs, edge.id) >= 0) continue;

                bool intersects = false;
                for (int i = 0; i < edge.points.Length - 1 && !intersects; i++)
                {
                    Vector2 p1 = new Vector2(edge.points[i].position.x, edge.points[i].position.z);
                    Vector2 p2 = new Vector2(edge.points[i+1].position.x, edge.points[i+1].position.z);
                    
                    for (int j = 0; j < potentialNextEdge.points.Length - 1 && !intersects; j++)
                    {
                        Vector2 q1 = new Vector2(potentialNextEdge.points[j].position.x, potentialNextEdge.points[j].position.z);
                        Vector2 q2 = new Vector2(potentialNextEdge.points[j+1].position.x, potentialNextEdge.points[j+1].position.z);

                        if (LineSegmentsIntersect(p1, p2, q1, q2))
                        {
                            intersects = true;
                        }
                    }
                }
                if (intersects) conflicts.Add(potentialNextEdge.id);
            }
            edge.conflictingEdgeIDs = conflicts.ToArray();
        }

        TrafficLightController[] allLights = FindObjectsByType<TrafficLightController>(FindObjectsSortMode.None);
        List<TrafficLightController> validLights = new List<TrafficLightController>();
        int lightIdCounter = 0;
        
        foreach (var light in allLights)
        {
            float minDistance = float.MaxValue;
            ushort closestEdge = 0xFFFF;
            foreach (var edge in _outputGraph.edges)
            {
                // Traffic light is placed at the stop line, which is the END of the edge
                Vector3 endPoint = edge.points[edge.points.Length - 1].position;
                float dist = Vector3.Distance(light.transform.position, endPoint);
                if (dist < minDistance && dist <_trafficLightSearchRadius)
                {
                    minDistance = dist;
                    closestEdge = edge.id;
                }
            }
            
            if (closestEdge != 0xFFFF)
            {
                light.lightId = lightIdCounter++;
                light.edgeId = closestEdge;
                EditorUtility.SetDirty(light);
                validLights.Add(light);
            }
        }

        TrafficLightController[] lights = validLights.ToArray();
        System.Array.Sort(lights, (a, b) => a.lightId.CompareTo(b.lightId));

        _outputGraph.intersections.Clear();
        List<TrafficLightController> unassignedLights = new List<TrafficLightController>(lights);

        while (unassignedLights.Count > 0)
        {
            TrafficLightController centerLight = unassignedLights[0];
            List<TrafficLightController> cluster = new List<TrafficLightController>();
            
            for (int i = unassignedLights.Count - 1; i >= 0; i--)
            {
                if (Vector3.Distance(centerLight.transform.position, unassignedLights[i].transform.position) <= 25f)
                {
                    cluster.Add(unassignedLights[i]);
                    unassignedLights.RemoveAt(i);
                }
            }

            List<int> phaseALights = new List<int>();
            List<int> phaseBLights = new List<int>();

            foreach (var l in cluster)
            {
                if (l.phase == TrafficLightController.LightPhase.PhaseA) phaseALights.Add(l.lightId);
                else phaseBLights.Add(l.lightId);
            }

            _outputGraph.intersections.Add(new IntersectionData
            {
                phaseALightIds = phaseALights.ToArray(),
                phaseBLightIds = phaseBLights.ToArray()
            });
        }

        TrafficManager manager = GetComponent<TrafficManager>();
        if (manager == null) manager = FindFirstObjectByType<TrafficManager>();
        
        if (manager != null)
        {
            manager.trafficLights = lights;
            EditorUtility.SetDirty(manager);
        }

        TrafficClient client = GetComponent<TrafficClient>();
        if (client == null) client = FindFirstObjectByType<TrafficClient>();
        
        if (client != null)
        {
            client.trafficLights = lights;
            EditorUtility.SetDirty(client);
        }

        EditorUtility.SetDirty(_outputGraph);
        AssetDatabase.SaveAssets();
        Debug.Log($"Bake completed: {_outputGraph.edges.Count} edges generated.");
#endif
    }
    private bool OnSegment(Vector2 p, Vector2 q, Vector2 r)
    {
        if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
            q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y))
            return true;
        return false;
    }

    private int Orientation(Vector2 p, Vector2 q, Vector2 r)
    {
        float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
        if (Mathf.Abs(val) < 0.001f) return 0;
        return (val > 0) ? 1 : 2;
    }

    private bool LineSegmentsIntersect(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
    {
        int o1 = Orientation(p1, q1, p2);
        int o2 = Orientation(p1, q1, q2);
        int o3 = Orientation(p2, q2, p1);
        int o4 = Orientation(p2, q2, q1);

        if (o1 != o2 && o3 != o4) return true;

        if (o1 == 0 && OnSegment(p1, p2, q1)) return true;
        if (o2 == 0 && OnSegment(p1, q2, q1)) return true;
        if (o3 == 0 && OnSegment(p2, p1, q2)) return true;
        if (o4 == 0 && OnSegment(p2, q1, q2)) return true;

        return false;
    }
}
