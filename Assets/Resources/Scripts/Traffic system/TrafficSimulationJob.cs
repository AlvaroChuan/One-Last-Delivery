using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using System.Threading;
using System.Linq;

[BurstCompile]
public struct TrafficSimulationJob : IJobParallelFor
{
    public NativeArray<NativeVehicle> vehicles;
    [ReadOnly] public NativeArray<NativeVehicle> previousStates;
    [ReadOnly] public NativeParallelMultiHashMap<int, int> vehicleMap;
    [ReadOnly] public NativeArray<NativeEdge> edges;
    [ReadOnly] public NativeArray<ushort> connections;
    [NativeDisableParallelForRestriction] public NativeArray<int> nodeLocks;
    [ReadOnly] public NativeArray<ushort> conflicts;
    public float deltaTime;
    public float maxSpeed;
    public float acceleration;
    public float safeDistance;
    public uint randomSeed;

    public void Execute(int index)
    {
        NativeVehicle vehicle = vehicles[index];
        NativeEdge currentEdge = edges[vehicle.currentEdgeId];
        float distanceToFront = float.MaxValue;
        vehicle.lastLaneChangeTime += deltaTime;

        if (vehicleMap.TryGetFirstValue(vehicle.currentEdgeId, out int otherIndex, out NativeParallelMultiHashMapIterator<int> it))
        {
            do
            {
                if (otherIndex == index) continue;
                
                NativeVehicle other = previousStates[otherIndex];
                float dist = other.distance - vehicle.distance;
                
                if (dist > 0 && dist < distanceToFront)
                {
                    distanceToFront = dist;
                }
            } while (vehicleMap.TryGetNextValue(out otherIndex, ref it));
        }

        bool canEnterIntersection = true;
        float distanceToEnd = currentEdge.length - vehicle.distance;

        if (distanceToFront < safeDistance * 2f && vehicle.lastLaneChangeTime >= 5f)
        {
            int targetEdgeIndex = -1;

            if (currentEdge.leftEdgeId != -1) targetEdgeIndex = currentEdge.leftEdgeId;
            else if (currentEdge.rightEdgeId != -1) targetEdgeIndex = currentEdge.rightEdgeId;

            if (targetEdgeIndex != -1)
            {
                float targetLaneDistToFront = float.MaxValue;
                float targetLaneDistToBack = float.MaxValue;
                bool safeToChange = true;

                if (vehicleMap.TryGetFirstValue(targetEdgeIndex, out int adjIndex, out NativeParallelMultiHashMapIterator<int> adjIt))
                {
                    do
                    {
                        NativeVehicle adjVehicle = previousStates[adjIndex];
                        float distDiff = adjVehicle.distance - vehicle.distance;

                        // Vehicle in target lane ahead
                        if (distDiff > 0 && distDiff < targetLaneDistToFront) targetLaneDistToFront = distDiff;
                        // Vehicle in target lane behind
                        else if (distDiff < 0 && math.abs(distDiff) < targetLaneDistToBack) targetLaneDistToBack = math.abs(distDiff);

                        // Parallel vehicle check
                        if (math.abs(distDiff) < (safeDistance * 0.5f))
                        {
                            safeToChange = false;
                            break;
                        }

                    } while (vehicleMap.TryGetNextValue(out adjIndex, ref adjIt));
                }

                // MOBIL (Safe to switch lane and we speed advantage)
                if (safeToChange && targetLaneDistToBack > safeDistance && targetLaneDistToFront > distanceToFront + (safeDistance * 1.5f))
                {
                    vehicle.currentEdgeId = targetEdgeIndex;

                    float lengthRatio = edges[targetEdgeIndex].length / currentEdge.length;
                    vehicle.distance *= lengthRatio;
                    
                    vehicle.lastLaneChangeTime = 0;
                    
                    currentEdge = edges[targetEdgeIndex];
                    distanceToFront = targetLaneDistToFront;
                }
            }
        }

        if (distanceToEnd < safeDistance && currentEdge.connectionCount > 0)
        {
            uint stableSeed = (uint)math.max(1, vehicle.id * 73856 + currentEdge.id * 19284);
            Random random = new Random(stableSeed);
            int nextEdgeOffset = random.NextInt(0, currentEdge.connectionCount);
            int nextEdgeId = connections[currentEdge.connectionStartIndex + nextEdgeOffset];
            NativeEdge nextEdge = edges[nextEdgeId];

            if (vehicleMap.TryGetFirstValue(nextEdgeId, out int nextIdx, out NativeParallelMultiHashMapIterator<int> nextIt))
            {
                do
                {
                    NativeVehicle nextVehicle = previousStates[nextIdx];
                    float dist = distanceToEnd + nextVehicle.distance;
                    if (dist > 0 && dist < distanceToFront) distanceToFront = dist;
                } while (vehicleMap.TryGetNextValue(out nextIdx, ref nextIt));
            }

            bool hasConflict = false;
            for (int i = 0; i < nextEdge.conflictCount; i++)
            {
                ushort conflictId = conflicts[nextEdge.conflictStartIndex + i];
                if (vehicleMap.TryGetFirstValue(conflictId, out int confIdx, out NativeParallelMultiHashMapIterator<int> confIt))
                {
                    hasConflict = true;
                    break;
                }
            }

            if (hasConflict)
            {
                canEnterIntersection = false;
                distanceToFront = math.min(distanceToFront, distanceToEnd);
            }
        }

        if (distanceToFront < safeDistance || !canEnterIntersection)
        {
            vehicle.speed -= acceleration * 2f * deltaTime; 
            if (vehicle.speed < 0f) vehicle.speed = 0f;
        }
        else
        {
            vehicle.speed += acceleration * deltaTime;
            if (vehicle.speed > maxSpeed) vehicle.speed = maxSpeed;
        }

        vehicle.distance += vehicle.speed * deltaTime;
        
        if (vehicle.distance >= currentEdge.length)
        {
            if (currentEdge.connectionCount > 0)
            {
                uint stableSeed = (uint)math.max(1, vehicle.id * 73856 + currentEdge.id * 19284);
                Random random = new Random(stableSeed);
                int nextEdgeOffset = random.NextInt(0, currentEdge.connectionCount);
                int nextEdgeId = connections[currentEdge.connectionStartIndex + nextEdgeOffset];

                vehicle.currentEdgeId = nextEdgeId; 
                vehicle.distance = 0f; 
            }
            else
            {
                vehicle.distance = currentEdge.length; 
                vehicle.speed = 0f; 
            }
        }
        vehicles[index] = vehicle;
    }
}