using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System.Threading;

[BurstCompile]
public struct TrafficSimulationJob : IJobParallelFor
{
    public NativeArray<NativeVehicle> vehicles;
    [ReadOnly] public NativeArray<NativeVehicle> previousstates;
    [ReadOnly] public NativeParallelMultiHashMap<int, int> vehicleMap;
    [ReadOnly] public NativeArray<NativeEdge> edges;
    [ReadOnly] public NativeArray<ushort> connections;
    [NativeDisableParallelForRestriction] public NativeArray<int> nodeLocks;
    public float deltaTime;
    public float maxSpeed;
    public float acceleration;
    public float safeDistance;
    public uint randomSeed;

    public void Execute(int index)
    {
        NativeVehicle vehicle = vehicles[index];
        NativeEdge currentEdge = edges[vehicle.currentEdgeIndex];
        float distanceToFront = float.MaxValue;

        if (vehicleMap.TryGetFirstValue(vehicle.currentEdgeIndex, out int otherIndex, out NativeParallelMultiHashMapIterator<int> it))
        {
            do
            {
                if (otherIndex == index) continue;
                
                NativeVehicle other = previousstates[otherIndex];
                float dist = other.distance - vehicle.distance;
                
                if (dist > 0 && dist < distanceToFront)
                {
                    distanceToFront = dist;
                }
            } while (vehicleMap.TryGetNextValue(out otherIndex, ref it));
        }

        bool canEnterIntersection = true;
        float distanceToEnd = currentEdge.length - vehicle.distance;

        if (distanceToEnd < safeDistance && currentEdge.endNodeID >= 0)
        {
            int lockValue = Interlocked.CompareExchange(ref nodeLocks.GetSubArray(currentEdge.endNodeID, 1).ToArray()[0], (int)vehicle.id + 1, 0);
            
            if (lockValue != 0 && lockValue != (int)vehicle.id + 1)
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
                Random random = new Random(randomSeed + (uint)index + 1);
                int randomConnectionOffset = random.NextInt(0, currentEdge.connectionCount);
                int nextEdgeId = connections[currentEdge.connectionStartIndex + randomConnectionOffset];

                vehicle.currentEdgeIndex = nextEdgeId; 
                vehicle.distance -= currentEdge.length; 
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