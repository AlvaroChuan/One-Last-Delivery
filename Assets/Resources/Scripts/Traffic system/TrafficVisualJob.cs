using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public struct NativeVisualNode
{
    public float3 position;
    public float3 tangent;
}

public struct NativeVisualEdge
{
    public int startIndex;
    public int pointCount;
    public float length;
}

public struct VehicleVisualState
{
    public int targetEdgeIndex;
    public float logicalDistance;
    public float networkSpeed;
    
    // Smooth variables
    public float currentLateralOffset;
    public float lateralVelocity;
    
    public float3 targetPosition;
    public quaternion targetRotation;
}

[BurstCompile]
public struct UpdateVehiclesVisualJob : IJobParallelForTransform
{
    [ReadOnly] public NativeArray<NativeVisualEdge> edges;
    [ReadOnly] public NativeArray<NativeVisualNode> nodes;
    public NativeArray<VehicleVisualState> states;
    
    public float deltaTime;
    public float laneChangeSmoothTime;
    public float teleportDistanceThreshold;
    public float overshootBrakeFactor;
    public float catchUpMultiplier;
    public float rotationSpeed;

    public void Execute(int index, TransformAccess transform)
    {
        VehicleVisualState state = states[index];
        if (state.targetEdgeIndex < 0 || state.targetEdgeIndex >= edges.Length) return;

        state.logicalDistance += state.networkSpeed * deltaTime;
        
        NativeVisualEdge edge = edges[state.targetEdgeIndex];
        
        float clampedDistance = math.clamp(state.logicalDistance, 0f, edge.length);
        float normalizedT = edge.length > 0 ? clampedDistance / edge.length : 0f;
        
        float floatIndex = normalizedT * (edge.pointCount - 1);
        int indexA = (int)math.floor(floatIndex);
        int indexB = math.min(indexA + 1, edge.pointCount - 1);
        float t = floatIndex - indexA;
        
        NativeVisualNode nodeA = nodes[edge.startIndex + indexA];
        NativeVisualNode nodeB = nodes[edge.startIndex + indexB];
        
        state.targetPosition = math.lerp(nodeA.position, nodeB.position, t);
        float3 dir = math.lerp(nodeA.tangent, nodeB.tangent, t);
        
        if (math.lengthsq(dir) > 0.001f)
        {
            state.targetRotation = quaternion.LookRotationSafe(dir, math.up());
            // Apply Dynamic Kinematic Steering for smooth lane changes
            if (math.abs(state.currentLateralOffset) > 0.01f)
            {
                float lookAheadDist = math.max(3.0f, state.networkSpeed * 0.6f);
                float steerAngle = math.atan(-state.currentLateralOffset / lookAheadDist);
                steerAngle = math.clamp(steerAngle, -0.6f, 0.6f); // clamp to roughly +/- 35 degrees
                state.targetRotation = math.mul(state.targetRotation, quaternion.AxisAngle(math.up(), steerAngle));
            }
        }
        
        // Extrapolate
        if (state.logicalDistance > edge.length)
        {
            float excessDistance = state.logicalDistance - edge.length;
            state.targetPosition += math.mul(state.targetRotation, new float3(0, 0, 1)) * excessDistance;
        }

        // Apply Smooth Lateral Offset
        if (math.abs(state.currentLateralOffset) > 0.01f)
        {
            // SmoothDamp manual implementation
            float omega = 2f / laneChangeSmoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float change = state.currentLateralOffset;
            float temp = (state.lateralVelocity + omega * change) * deltaTime;
            state.lateralVelocity = (state.lateralVelocity - omega * temp) * exp;
            state.currentLateralOffset = change * exp + temp * exp;
            
            float3 rightVec = math.normalize(math.cross(math.up(), dir));
            state.targetPosition += rightVec * state.currentLateralOffset;
        }

        float3 directionToTarget = state.targetPosition - (float3)transform.position;
        float distanceToTarget = math.length(directionToTarget);
        float3 previousPosition = transform.position;
        float actualSpeed = state.networkSpeed;

        if (distanceToTarget > teleportDistanceThreshold)
        {
            transform.position = state.targetPosition;
            transform.rotation = state.targetRotation;
            actualSpeed = 0f;
        }
        else if (distanceToTarget > 0.05f)
        {
            directionToTarget /= distanceToTarget;
            float3 forward = math.mul(transform.rotation, new float3(0, 0, 1));
            float forwardDot = math.dot(forward, directionToTarget);
            
            if (forwardDot < 0f)
            {
                actualSpeed = state.networkSpeed * overshootBrakeFactor;
                transform.position += (Vector3)(math.mul(state.targetRotation, new float3(0, 0, 1)) * (actualSpeed * deltaTime));
            }
            else
            {
                actualSpeed = state.networkSpeed + (distanceToTarget * catchUpMultiplier);
                
                // Vector3.MoveTowards manual implementation
                float3 a = transform.position;
                float3 b = state.targetPosition;
                float3 vector = b - a;
                float mag = math.length(vector);
                float maxDist = actualSpeed * deltaTime;
                if (mag <= maxDist || mag == 0f) transform.position = b;
                else transform.position = a + vector / mag * maxDist;
            }

            // The sideways delta rotation override was removed to prevent 90-degree snapping
        }

        if (distanceToTarget <= teleportDistanceThreshold)
        {
            // Always allow smooth rotation based on time, so cars can straighten their wheels when stopped
            transform.rotation = math.slerp(transform.rotation, state.targetRotation, math.clamp(deltaTime * rotationSpeed * 2f, 0f, 1f));
        }
        
        states[index] = state;
    }
}
