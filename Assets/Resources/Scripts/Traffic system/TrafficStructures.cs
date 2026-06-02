using UnityEngine;
using Mirror;

public struct NativeVehicle
{
    public uint id;
    public int currentEdgeIndex;
    public float distance;
    public float speed;
}

public struct NativeEdge
{
    public ushort id;
    public float length;
    public int connectionStartIndex;
    public int connectionCount;
    public int endNodeID;
}

public struct NetworkVehicleState
{
    public uint id;
    public int currentEdgeIndex;
    public float distance;
}

public struct TrafficBatchMessage : NetworkMessage
{
    public NetworkVehicleState[] vehicles;
}
