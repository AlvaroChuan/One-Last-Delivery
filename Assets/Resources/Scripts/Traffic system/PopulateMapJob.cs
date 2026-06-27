using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public struct PopulateMapJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<NativeVehicle> vehicles;
    public NativeParallelMultiHashMap<int, int>.ParallelWriter edgeMap;

    public void Execute(int index)
    {
        NativeVehicle vehicle = vehicles[index];
        if (vehicle.currentEdgeId == -1) return; // Ignore inactive vehicles
        edgeMap.Add(vehicle.currentEdgeId, index);
    }
}