using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct EdgePoint
{
    public Vector3 position;
    public Vector3 tangent;
}

[Serializable]
public class TrafficEdge
{
    public ushort id;
    public float length;
    public ushort[] nextEdgeIDs;
    public EdgePoint[] points;
    public ushort[] conflictingEdgeIDs;
    public int leftEdgeId = -1;
    public int rightEdgeId = -1;
}

[CreateAssetMenu(fileName = "TrafficGraph", menuName = "Traffic/Traffic Graph")]
public class TrafficGraph : ScriptableObject
{
    public List<TrafficEdge> edges = new List<TrafficEdge>();
}
