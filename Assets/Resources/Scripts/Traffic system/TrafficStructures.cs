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

public struct NativeObstacle
{
    public uint id;
    public float distance;
    public int edgeId;
}

public struct NativeVehicleConfig
{
    public float maxSpeed;
    public float acceleration;
    public float safeDistance;
}

public struct DynamicObstacleData
{
    public uint id;
    public Unity.Mathematics.float3 position;
}

public struct NativeEdge
{
    public ushort id;
    public float length;
    public int connectionStartIndex;
    public int connectionCount;
    public int conflictStartIndex;
    public int conflictCount;
    public int pointsStartIndex;
    public int pointsCount;
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

public struct ServerCarCrashCarMessage : NetworkMessage
{
    public uint carId;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 impactVelocity;
}

public struct ClientCarCrashCarMessage : NetworkMessage
{
    public uint carId;
}
