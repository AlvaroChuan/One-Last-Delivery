using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public struct ClearLocksJob : IJobParallelFor
{
    public NativeArray<int> locks;

    public void Execute(int index)
    {
        locks[index] = 0;
    }
}