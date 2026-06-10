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
    public int conflictStartIndex;
    public int conflictCount;
    public int leftEdgeId;
    public int rightEdgeId;
}

public struct NativeIntersection
{
    public int phaseAStartIndex;
    public int phaseACount;
    public int phaseBStartIndex;
    public int phaseBCount;
    public float currentTimer;
    public int currentStep;
}

public struct NetworkVehicleState
{
    public uint id;
    public int currentEdgeId;
    public float distance;
    public float speed;
}

public struct TrafficBatchMessage : NetworkMessage
{
    public NetworkVehicleState[] vehicles;
    public byte[] lightStates;
}
