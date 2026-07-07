using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public struct MapObstaclesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<DynamicObstacleData> inputObstacles;
    [ReadOnly] public NativeParallelMultiHashMap<int, int> spatialGrid;
    [ReadOnly] public NativeArray<NativeEdge> edges;
    [ReadOnly] public NativeArray<EdgePoint> allPoints;
    
    public float cellSize;
    public float snapDistance;
    [WriteOnly] public NativeParallelMultiHashMap<int, float>.ParallelWriter outputObstacles;

    public void Execute(int index)
    {
        DynamicObstacleData data = inputObstacles[index];
        
        int gridX = (int)math.floor(data.position.x / cellSize);
        int gridZ = (int)math.floor(data.position.z / cellSize);

        // Check adjacent cells since snapDistance might extend beyond one cell
        int searchRadius = math.max(1, (int)math.ceil(snapDistance / cellSize));

        // Keep track of processed edges to avoid duplicate mapping for the same obstacle
        NativeList<int> processedEdges = new NativeList<int>(16, Allocator.Temp);

        for (int dx = -searchRadius; dx <= searchRadius; dx++)
        {
            for (int dz = -searchRadius; dz <= searchRadius; dz++)
            {
                int checkX = gridX + dx;
                int checkZ = gridZ + dz;
                int gridKey = (checkX * 73856) ^ (checkZ * 19284);

                if (spatialGrid.TryGetFirstValue(gridKey, out int edgeId, out NativeParallelMultiHashMapIterator<int> it))
                {
                    do
                    {
                        if (processedEdges.Contains(edgeId)) continue;
                        processedEdges.Add(edgeId);

                        NativeEdge edge = edges[edgeId];
                        float accumulatedDistance = 0f;
                        float bestDistToSegment = float.MaxValue;
                        float bestDistanceAlongEdge = 0f;

                        for (int i = 0; i < edge.pointsCount - 1; i++)
                        {
                            float3 p1 = allPoints[edge.pointsStartIndex + i].position;
                            float3 p2 = allPoints[edge.pointsStartIndex + i + 1].position;

                            float3 lineVec = p2 - p1;
                            float segmentLen = math.length(lineVec);
                            float3 lineDir = segmentLen > 0f ? lineVec / segmentLen : float3.zero;

                            float3 pointVec = data.position - p1;
                            float t = segmentLen > 0f ? math.dot(pointVec, lineDir) : 0f;
                            t = math.clamp(t, 0f, segmentLen);

                            float3 closestPoint = p1 + lineDir * t;
                            float distToSegment = math.distance(data.position, closestPoint);

                            if (distToSegment < bestDistToSegment)
                            {
                                bestDistToSegment = distToSegment;
                                bestDistanceAlongEdge = accumulatedDistance + t;
                            }

                            accumulatedDistance += segmentLen;
                        }

                        // If the edge is within snapDistance, map the obstacle to it!
                        if (bestDistToSegment <= snapDistance)
                        {
                            outputObstacles.Add(edgeId, bestDistanceAlongEdge);
                        }

                    } while (spatialGrid.TryGetNextValue(out edgeId, ref it));
                }
            }
        }
        
        processedEdges.Dispose();
    }
}
