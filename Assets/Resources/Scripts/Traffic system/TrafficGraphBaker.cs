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
    [SerializeField] float _intersectionClusterRadius = 50.0f;
    [SerializeField] float _laneDistanceThreshold = 5.0f;
    [SerializeField] float _knotMergeRadius = 1.0f;
    [SerializeField] bool _showGizmos = true;

    private struct EndpointData
    {
        public SplineContainer container;
        public Spline spline;
        public int knotIndex;
        public Vector3 worldPos;
    }

    private struct SplineRef
    {
        public SplineContainer container;
        public Spline spline;
    }

    [ContextMenu("Merge Spline Knots (Heal Graph)")]
    public void MergeSplineKnots()
    {
#if UNITY_EDITOR
        if (_splineContainers == null || _splineContainers.Length == 0) return;

        Undo.RecordObjects(_splineContainers, "Merge Spline Knots");

        // 0. Remove Duplicate Splines
        List<SplineRef> allSplines = new List<SplineRef>();
        foreach (var container in _splineContainers)
        {
            if (container == null) continue;
            foreach (var spline in container.Splines)
            {
                allSplines.Add(new SplineRef { container = container, spline = spline });
            }
        }

        HashSet<Spline> splinesToDelete = new HashSet<Spline>();

        for (int i = 0; i < allSplines.Count; i++)
        {
            var refA = allSplines[i];
            if (splinesToDelete.Contains(refA.spline)) continue;

            for (int j = i + 1; j < allSplines.Count; j++)
            {
                var refB = allSplines[j];
                if (splinesToDelete.Contains(refB.spline)) continue;

                if (refA.spline.Count != refB.spline.Count) continue;

                bool isDuplicate = true;
                for (int k = 0; k < refA.spline.Count; k++)
                {
                    Vector3 posA = refA.container.transform.TransformPoint(refA.spline[k].Position);
                    Vector3 posB = refB.container.transform.TransformPoint(refB.spline[k].Position);
                    
                    if (Vector3.Distance(posA, posB) > 0.1f) // 10cm threshold for identical knots
                    {
                        isDuplicate = false;
                        break;
                    }
                }

                if (isDuplicate)
                {
                    splinesToDelete.Add(refB.spline);
                }
            }
        }

        int duplicatesRemoved = 0;
        foreach (var container in _splineContainers)
        {
            if (container == null) continue;
            // Iterate backwards to safely remove from the container
            for (int i = container.Splines.Count - 1; i >= 0; i--)
            {
                if (splinesToDelete.Contains(container.Splines[i]))
                {
                    container.RemoveSpline(container.Splines[i]);
                    EditorUtility.SetDirty(container);
                    duplicatesRemoved++;
                }
            }
        }

        List<EndpointData> endpoints = new List<EndpointData>();

        // 1. Collect all start and end knots
        foreach (var container in _splineContainers)
        {
            if (container == null) continue;
            foreach (var spline in container.Splines)
            {
                if (spline.Count < 2) continue;

                // Start knot
                var startKnot = spline[0];
                endpoints.Add(new EndpointData
                {
                    container = container,
                    spline = spline,
                    knotIndex = 0,
                    worldPos = container.transform.TransformPoint(startKnot.Position)
                });

                // End knot
                var endKnot = spline[spline.Count - 1];
                endpoints.Add(new EndpointData
                {
                    container = container,
                    spline = spline,
                    knotIndex = spline.Count - 1,
                    worldPos = container.transform.TransformPoint(endKnot.Position)
                });
            }
        }

        // 2. Cluster them by distance
        List<List<EndpointData>> clusters = new List<List<EndpointData>>();
        bool[] processed = new bool[endpoints.Count];

        for (int i = 0; i < endpoints.Count; i++)
        {
            if (processed[i]) continue;

            List<EndpointData> currentCluster = new List<EndpointData>();
            currentCluster.Add(endpoints[i]);
            processed[i] = true;

            for (int j = i + 1; j < endpoints.Count; j++)
            {
                if (processed[j]) continue;

                if (Vector3.Distance(endpoints[i].worldPos, endpoints[j].worldPos) <= _knotMergeRadius)
                {
                    currentCluster.Add(endpoints[j]);
                    processed[j] = true;
                }
            }

            if (currentCluster.Count > 1)
            {
                clusters.Add(currentCluster);
            }
        }

        // 3. Apply the average position to all knots in the cluster
        int mergedCount = 0;
        foreach (var cluster in clusters)
        {
            Vector3 averagePos = Vector3.zero;
            foreach (var ep in cluster)
            {
                averagePos += ep.worldPos;
            }
            averagePos /= cluster.Count;

            foreach (var ep in cluster)
            {
                var knot = ep.spline[ep.knotIndex];
                knot.Position = (Unity.Mathematics.float3)ep.container.transform.InverseTransformPoint(averagePos);
                ep.spline.SetKnot(ep.knotIndex, knot);
                EditorUtility.SetDirty(ep.container);
            }
            mergedCount++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Heal Graph completed: Removed {duplicatesRemoved} duplicate splines. Merged {mergedCount} knot clusters.");
#endif
    }

    [ContextMenu("Fix Traffic Light Pivot Offsets")]
    public void FixTrafficLightPivots()
    {
#if UNITY_EDITOR
        TrafficLightController[] allLights = FindObjectsByType<TrafficLightController>(FindObjectsSortMode.None);
        int fixedCount = 0;

        foreach (var light in allLights)
        {
            if (light.transform.childCount == 0) continue;

            // Collect all children and their world positions
            Transform[] children = new Transform[light.transform.childCount];
            Vector3[] childWorldPos = new Vector3[light.transform.childCount];
            Quaternion[] childWorldRot = new Quaternion[light.transform.childCount];

            Vector3 centerPos = Vector3.zero;
            for (int i = 0; i < light.transform.childCount; i++)
            {
                children[i] = light.transform.GetChild(i);
                childWorldPos[i] = children[i].position;
                childWorldRot[i] = children[i].rotation;
                centerPos += children[i].position;
            }
            centerPos /= light.transform.childCount;

            // If the parent is already at the center, skip
            if (Vector3.Distance(light.transform.position, centerPos) < 0.1f) continue;

            Undo.RecordObject(light.transform, "Fix Traffic Light Pivots");
            for (int i = 0; i < children.Length; i++) Undo.RecordObject(children[i], "Fix Traffic Light Pivots");

            // Move parent to the center of its children
            light.transform.position = centerPos;

            // Restore children to their original world positions
            for (int i = 0; i < children.Length; i++)
            {
                children[i].position = childWorldPos[i];
                children[i].rotation = childWorldRot[i];
            }

            EditorUtility.SetDirty(light.gameObject);
            fixedCount++;
        }

        Debug.Log($"[TrafficGraphBaker] Fixed pivot offsets for {fixedCount} traffic lights! Parent pivots are now perfectly centered on their visual meshes.");
#endif
    }

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

        // Detect Left and Right Lanes
        foreach (var edge in _outputGraph.edges)
        {
            edge.leftEdgeId = -1;
            edge.rightEdgeId = -1;

            Vector3 startPos = edge.points[0].position;
            Vector3 endPos = edge.points[edge.points.Length - 1].position;
            Vector3 startTangent = edge.points[0].tangent;

            float minRightDist = float.MaxValue;
            float minLeftDist = float.MaxValue;

            foreach (var potentialLane in _outputGraph.edges)
            {
                if (edge.id == potentialLane.id) continue;

                Vector3 otherStart = potentialLane.points[0].position;
                Vector3 otherEnd = potentialLane.points[potentialLane.points.Length - 1].position;
                Vector3 otherTangent = potentialLane.points[0].tangent;

                float startDist = Vector3.Distance(startPos, otherStart);
                float endDist = Vector3.Distance(endPos, otherEnd);

                if (startDist <= _laneDistanceThreshold && startDist > 0.5f && endDist <= _laneDistanceThreshold && endDist > 0.5f)
                {
                    // Ensure they flow in the exact same direction
                    if (Vector3.Dot(startTangent, otherTangent) > 0.8f)
                    {
                        Vector3 toOther = (otherStart - startPos).normalized;
                        Vector3 cross = Vector3.Cross(startTangent, toOther);

                        // Y > 0 means it's on the right, Y < 0 means it's on the left
                        if (cross.y > 0 && startDist < minRightDist)
                        {
                            minRightDist = startDist;
                            edge.rightEdgeId = potentialLane.id;
                        }
                        else if (cross.y < 0 && startDist < minLeftDist)
                        {
                            minLeftDist = startDist;
                            edge.leftEdgeId = potentialLane.id;
                        }
                    }
                }
            }
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
        HashSet<ushort> assignedEdges = new HashSet<ushort>();
        
        foreach (var light in allLights)
        {
            List<ushort> controlledEdges = new List<ushort>();
            Vector3 center = light.transform.position + light.transform.TransformVector(light.searchOffset);

            foreach (var edge in _outputGraph.edges)
            {
                // Traffic light is placed at the stop line, which is the END of the edge
                Vector3 endPoint = edge.points[edge.points.Length - 1].position;
                float dist = Vector3.Distance(center, endPoint);
                if (dist < _trafficLightSearchRadius && !assignedEdges.Contains(edge.id))
                {
                    controlledEdges.Add(edge.id);
                }
            }
            
            if (controlledEdges.Count > 0)
            {
                foreach(var id in controlledEdges) assignedEdges.Add(id);
                Undo.RecordObject(light, "Assign Traffic Light ID");
                light.lightId = lightIdCounter++;
                light.edgeIds = controlledEdges.ToArray();
                EditorUtility.SetDirty(light);
                PrefabUtility.RecordPrefabInstancePropertyModifications(light);
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
                if (Vector3.Distance(centerLight.transform.position, unassignedLights[i].transform.position) <= _intersectionClusterRadius)
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

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!_showGizmos) return;

        TrafficLightController[] allLights = FindObjectsByType<TrafficLightController>(FindObjectsSortMode.None);
        
        foreach (var light in allLights)
        {
            Vector3 center = light.transform.position + light.transform.TransformVector(light.searchOffset);
            
            // Draw the Search Radius (How far it looks for a lane)
            Handles.color = new Color(1f, 1f, 0f, 0.5f); // Semi-transparent yellow
            Handles.DrawWireDisc(center, Vector3.up, _trafficLightSearchRadius);

            // Draw the Cluster Radius (How far it looks for other lights)
            Handles.color = new Color(0f, 1f, 1f, 0.2f); // Semi-transparent cyan
            Handles.DrawWireDisc(light.transform.position, Vector3.up, _intersectionClusterRadius);
        }
    }
#endif
}
