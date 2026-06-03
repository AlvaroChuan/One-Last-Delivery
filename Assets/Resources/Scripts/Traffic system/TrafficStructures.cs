using UnityEngine;
using Mirror;

public struct NativeVehicle
{
    public uint id;
    public int currentEdgeId;
    public float distance;
    public float speed;
    public float lastLaneChangeTime;
}

public struct NativeEdge
{
    public ushort id;
    public float length;
    public int connectionStartIndex;
    public int connectionCount;
    public int endNodeID;
    public int leftEdgeId;
    public int rightEdgeId;
}

public struct NetworkVehicleState
{
    public uint id;
    public int currentEdgeId;
    public float distance;
    public float speed;
    public float lastLaneChangeTime;
}

public struct TrafficBatchMessage : NetworkMessage
{
    public NetworkVehicleState[] vehicles;
}
