using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public struct TrafficLightSimulationJob : IJobParallelFor
{
    public NativeArray<NativeIntersection> intersections;
    [ReadOnly] public NativeArray<int> intersectionLightIds;
    [ReadOnly] public NativeArray<ushort> lightToEdgeMapping; // Flat array of all controlled edges
    [ReadOnly] public NativeArray<int> lightToEdgeStartIndex;
    [ReadOnly] public NativeArray<int> lightToEdgeCount;
    
    [NativeDisableParallelForRestriction] public NativeArray<byte> lightStates;
    [NativeDisableParallelForRestriction] public NativeArray<byte> edgeStopSignals;

    public float deltaTime;
    public float greenTime;
    public float yellowTime;
    public float bothRedTime;

    public void Execute(int index)
    {
        NativeIntersection intersection = intersections[index];
        intersection.currentTimer -= deltaTime;

        if (intersection.currentTimer <= 0f)
        {
            intersection.currentStep = (intersection.currentStep + 1) % 6;

            switch (intersection.currentStep)
            {
                case 0: // Phase A Green
                    intersection.currentTimer = greenTime;
                    break;
                case 1: // Phase A Yellow
                    intersection.currentTimer = yellowTime;
                    break;
                case 2: // All Red
                    intersection.currentTimer = bothRedTime;
                    break;
                case 3: // Phase B Green
                    intersection.currentTimer = greenTime;
                    break;
                case 4: // Phase B Yellow
                    intersection.currentTimer = yellowTime;
                    break;
                case 5: // All Red
                    intersection.currentTimer = bothRedTime;
                    break;
            }
        }

        intersections[index] = intersection;

        // Apply visual states and physical stoppers
        byte phaseAState = 2; // Red
        byte phaseBState = 2; // Red

        if (intersection.currentStep == 0) phaseAState = 0; // Green
        else if (intersection.currentStep == 1) phaseAState = 1; // Yellow
        
        if (intersection.currentStep == 3) phaseBState = 0; // Green
        else if (intersection.currentStep == 4) phaseBState = 1; // Yellow

        // Write Phase A
        for (int i = 0; i < intersection.phaseACount; i++)
        {
            int lightId = intersectionLightIds[intersection.phaseAStartIndex + i];
            lightStates[lightId] = phaseAState;
            
            int startIdx = lightToEdgeStartIndex[lightId];
            int count = lightToEdgeCount[lightId];
            for (int e = 0; e < count; e++)
            {
                ushort edgeId = lightToEdgeMapping[startIdx + e];
                edgeStopSignals[edgeId] = (phaseAState == 1 || phaseAState == 2) ? (byte)1 : (byte)0;
            }
        }

        // Write Phase B
        for (int i = 0; i < intersection.phaseBCount; i++)
        {
            int lightId = intersectionLightIds[intersection.phaseBStartIndex + i];
            lightStates[lightId] = phaseBState;
            
            int startIdx = lightToEdgeStartIndex[lightId];
            int count = lightToEdgeCount[lightId];
            for (int e = 0; e < count; e++)
            {
                ushort edgeId = lightToEdgeMapping[startIdx + e];
                edgeStopSignals[edgeId] = (phaseBState == 1 || phaseBState == 2) ? (byte)1 : (byte)0;
            }
        }
    }
}