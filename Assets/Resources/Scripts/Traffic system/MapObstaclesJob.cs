using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct MapObstaclesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<DynamicObstacleData> inputObstacles;
    [ReadOnly] public NativeParallelMultiHashMap<int, int> spatialGrid;
    [ReadOnly] public NativeArray<NativeEdge> edges;
    [ReadOnly] public NativeArray<EdgePoint> allPoints;
    
    public float cellSize;
    public float snapDistance;
    public NativeArray<NativeObstacle> outputObstacles;

    public void Execute(int index)
    {
        DynamicObstacleData data = inputObstacles[index];
        
        int gridX = (int)math.floor(data.position.x / cellSize);
        int gridZ = (int)math.floor(data.position.z / cellSize);
        int gridKey = (gridX * 73856) ^ (gridZ * 19284);

        int bestEdgeId = -1;
        float bestDistanceToPoint = float.MaxValue;
        float distanceAlongEdge = 0f;

        if (spatialGrid.TryGetFirstValue(gridKey, out int edgeId, out NativeParallelMultiHashMapIterator<int> it))
        {
            do
            {
                NativeEdge edge = edges[edgeId];
                float accumulatedDistance = 0f;

                for (int i = 0; i < edge.pointsCount - 1; i++)
                {
                    float3 p1 = allPoints[edge.pointsStartIndex + i].position;
                    float3 p2 = allPoints[edge.pointsStartIndex + i + 1].position;

                    float3 lineVec = p2 - p1;
                    float segmentLen = math.length(lineVec);
                    float3 lineDir = lineVec / segmentLen;

                    float3 pointVec = data.position - p1;
                    float t = math.dot(pointVec, lineDir);
                    t = math.clamp(t, 0f, segmentLen);

                    float3 closestPoint = p1 + lineDir * t;
                    float distToSegment = math.distance(data.position, closestPoint);

                    if (distToSegment < bestDistanceToPoint)
                    {
                        bestDistanceToPoint = distToSegment;
                        bestEdgeId = edgeId;
                        distanceAlongEdge = accumulatedDistance + t;
                    }

                    accumulatedDistance += segmentLen;
                }
            } while (spatialGrid.TryGetNextValue(out edgeId, ref it));
        }

        // If the obstacle is more than snapDistance away from any edge, it shouldn't affect traffic
        if (bestDistanceToPoint > snapDistance)
        {
            bestEdgeId = -1;
        }

        outputObstacles[index] = new NativeObstacle
        {
            edgeId = bestEdgeId,
            distance = distanceAlongEdge
        };
    }
}
