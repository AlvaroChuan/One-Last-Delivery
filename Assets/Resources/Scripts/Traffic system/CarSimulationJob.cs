using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using System.Threading;

[BurstCompile]
public struct CarSimulationJob : IJobParallelFor
{
    public NativeArray<NativeVehicle> vehicles;
    [ReadOnly] public NativeArray<NativeVehicle> previousStates;
    [ReadOnly] public NativeParallelMultiHashMap<int, int> vehicleMap;
    [ReadOnly] public NativeArray<NativeEdge> edges;
    [ReadOnly] public NativeArray<ushort> connections;
    [NativeDisableParallelForRestriction] public NativeArray<int> nodeLocks;
    [ReadOnly] public NativeArray<ushort> conflicts;
    [ReadOnly] public NativeArray<byte> edgeStopSignals;
    [ReadOnly] public NativeArray<NativeObstacle> dynamicObstacles;
    [ReadOnly] public NativeArray<NativeVehicleConfig> vehicleConfigs;
    public float deltaTime;
    public uint randomSeed;

    public void Execute(int index)
    {
        NativeVehicle vehicle = previousStates[index];
        if (vehicle.currentEdgeId == -1)
        {
            vehicles[index] = vehicle; // Write back the inactive state
            return;
        }

        NativeEdge currentEdge = edges[vehicle.currentEdgeId];
        NativeVehicleConfig config = vehicleConfigs[(int)(vehicle.id % vehicleConfigs.Length)];
        float maxSpeed = config.maxSpeed;
        float acceleration = config.acceleration;
        float safeDistance = config.safeDistance;

        float distanceToFront = float.MaxValue;
        vehicle.lastLaneChangeTime += deltaTime;

        // Check vehicles on the same lane (go through all the cars on the same edge and find the closest one in front)
        if (vehicleMap.TryGetFirstValue(vehicle.currentEdgeId, out int otherIndex, out NativeParallelMultiHashMapIterator<int> it))
        {
            do
            {
                if (otherIndex == index) continue;
                NativeVehicle other = previousStates[otherIndex];
                float dist = other.distance - vehicle.distance;
                if (dist > 0)
                {
                    NativeVehicleConfig otherConfig = vehicleConfigs[(int)(other.id % vehicleConfigs.Length)];
                    dist -= math.max(0f, otherConfig.safeDistance - safeDistance);
                    if (dist < distanceToFront) distanceToFront = dist;
                }

            } while (vehicleMap.TryGetNextValue(out otherIndex, ref it));
        }


        // Lane changing logic (MOBIL inspired) NOT WORKING CHECK IN THE FUTURE
        bool canEnterIntersection = true;
        float distanceToEnd = currentEdge.length - vehicle.distance;

        // Virtual Obstacle for Traffic Lights
        if (edgeStopSignals[vehicle.currentEdgeId] == 1)
        {
            float distToLight = currentEdge.length - vehicle.distance;
            if (distToLight > 0 && distToLight < distanceToFront)
            {
                distanceToFront = distToLight;
            }
        }

        // Virtual Obstacles for Players and Dynamics
        for (int i = 0; i < dynamicObstacles.Length; i++)
        {
            if (dynamicObstacles[i].edgeId == vehicle.currentEdgeId)
            {
                float distToObs = dynamicObstacles[i].distance - vehicle.distance;
                if (distToObs > 0 && distToObs < distanceToFront)
                {
                    distanceToFront = distToObs;
                }
            }
        }

        if (vehicle.speed < maxSpeed * 0.9f && vehicle.lastLaneChangeTime >= 2.5f && distanceToEnd > 10f)
        {
            int bestTargetEdge = -1;
            float bestTargetLaneDistToFront = distanceToFront + (safeDistance * 1.5f); // Must be strictly better than this

            NativeArray<int> adjacentLanes = new NativeArray<int>(2, Allocator.Temp);
            adjacentLanes[0] = currentEdge.leftEdgeId;
            adjacentLanes[1] = currentEdge.rightEdgeId;

            for (int i = 0; i < 2; i++)
            {
                int laneEdgeId = adjacentLanes[i];
                if (laneEdgeId == -1) continue;

                float targetLaneDistToFront = float.MaxValue;
                float targetLaneDistToBack = float.MaxValue;
                bool safeToChange = true;

                if (vehicleMap.TryGetFirstValue(laneEdgeId, out int adjIndex, out NativeParallelMultiHashMapIterator<int> adjIt))
                {
                    do
                    {
                        NativeVehicle adjVehicle = previousStates[adjIndex];
                        // Need to adjust for length differences to compare distances safely
                        float ratio = edges[laneEdgeId].length / currentEdge.length;
                        float myEquivalentDist = vehicle.distance * ratio;
                        float distDiff = adjVehicle.distance - myEquivalentDist;

                        NativeVehicleConfig otherConfig = vehicleConfigs[(int)(adjVehicle.id % vehicleConfigs.Length)];
                        float requiredSafeGap = math.max(safeDistance, otherConfig.safeDistance);

                        if (distDiff > 0)
                        {
                            float effectiveDist = distDiff - math.max(0f, otherConfig.safeDistance - safeDistance);
                            if (effectiveDist < targetLaneDistToFront) targetLaneDistToFront = effectiveDist;
                        }
                        else if (distDiff < 0 && math.abs(distDiff) < targetLaneDistToBack)
                        {
                            targetLaneDistToBack = math.abs(distDiff);
                        }

                        if (math.abs(distDiff) < requiredSafeGap * 1.5f)
                        {
                            safeToChange = false;
                            break;
                        }
                    } while (vehicleMap.TryGetNextValue(out adjIndex, ref adjIt));
                }

                if (safeToChange && targetLaneDistToBack > safeDistance && targetLaneDistToFront > bestTargetLaneDistToFront)
                {
                    bestTargetLaneDistToFront = targetLaneDistToFront;
                    bestTargetEdge = laneEdgeId;
                }
            }

            adjacentLanes.Dispose();

            if (bestTargetEdge != -1)
            {
                vehicle.currentEdgeId = bestTargetEdge;
                float lengthRatio = edges[bestTargetEdge].length / currentEdge.length;
                vehicle.distance *= lengthRatio;
                vehicle.lastLaneChangeTime = 0;

                currentEdge = edges[bestTargetEdge];
                distanceToFront = bestTargetLaneDistToFront;
            }
        }

        // Intersection logic: Check if we are close to the end of the edge and if we can enter the intersection (no conflicts or we have priority)
        float lookAhead = math.max(safeDistance, (vehicle.speed * vehicle.speed) / (2f * acceleration * 2f) + 2f);
        bool isAtRedLight = edgeStopSignals[vehicle.currentEdgeId] == 1;

        int chosenNextEdgeId = -1;

        if (distanceToEnd < lookAhead && currentEdge.connectionCount > 0 && !isAtRedLight)
        {
            uint stableSeed = (uint)math.max(1, vehicle.id * 73856 + currentEdge.id * 19284);
            Random random = new Random(stableSeed);
            int startOffset = random.NextInt(0, currentEdge.connectionCount);
            
            bool finalHasConflict = true;
            NativeEdge finalNextEdge = default;

            for (int attempt = 0; attempt < currentEdge.connectionCount; attempt++)
            {
                int offset = (startOffset + attempt) % currentEdge.connectionCount;
                int candidateEdgeId = connections[currentEdge.connectionStartIndex + offset];
                NativeEdge candidateEdge = edges[candidateEdgeId];
                
                bool hasConflict = false;
                
                // Only check conflicts if we are NOT already in the intersection
                if (currentEdge.conflictCount == 0)
                {
                    unsafe
                    {
                        int* locksPtr = (int*)nodeLocks.GetUnsafePtr();
                        int myLockValue = (int)currentEdge.id + 1; // Use approach lane ID to share lock with vehicles behind

                        int currentLock = Interlocked.CompareExchange(ref locksPtr[candidateEdgeId], myLockValue, 0);
                        bool ownsLock = currentLock == 0 || currentLock == myLockValue;

                        if (!ownsLock) hasConflict = true;
                        else
                        {
                            for (int i = 0; i < candidateEdge.conflictCount; i++)
                            {
                                ushort conflictId = conflicts[candidateEdge.conflictStartIndex + i];

                                int confLock = Interlocked.CompareExchange(ref locksPtr[conflictId], 0, 0);
                                if (confLock != 0 && confLock != myLockValue)
                                {
                                    if (confLock < myLockValue) // In case of tie, lower edge ID wins
                                    {
                                        hasConflict = true;
                                        break;
                                    }
                                }

                                if (vehicleMap.TryGetFirstValue(conflictId, out int confIdx, out NativeParallelMultiHashMapIterator<int> confIt))
                                {
                                    // Deadlock fix: If the conflicting edge has a RED light, cars on it are parked. Ignore them!
                                    if (edgeStopSignals[conflictId] != 1)
                                    {
                                        hasConflict = true;
                                        break;
                                    }
                                }
                            }

                            if (hasConflict && currentLock == 0)
                            {
                                // Only release if WE were the ones who acquired it (not a shared lock from another car ahead)
                                Interlocked.CompareExchange(ref locksPtr[candidateEdgeId], 0, myLockValue);
                            }
                        }
                    }
                }
                
                if (!hasConflict)
                {
                    chosenNextEdgeId = candidateEdgeId;
                    finalNextEdge = candidateEdge;
                    finalHasConflict = false;
                    break; // Found an open path!
                }
            }

            // If ALL paths are blocked, default to the original intended path so we wait at the correct spot
            if (chosenNextEdgeId == -1)
            {
                chosenNextEdgeId = connections[currentEdge.connectionStartIndex + startOffset];
                finalNextEdge = edges[chosenNextEdgeId];
                finalHasConflict = true;
            }

            int nextEdgeId = chosenNextEdgeId;
            NativeEdge nextEdge = finalNextEdge;

            // Check for vehicles in the chosen next edge to brake if necessary
            if (vehicleMap.TryGetFirstValue(nextEdgeId, out int nextIdx, out NativeParallelMultiHashMapIterator<int> nextIt))
            {
                do
                {
                    NativeVehicle nextVehicle = previousStates[nextIdx];
                    float dist = distanceToEnd + nextVehicle.distance;
                    if (dist > 0)
                    {
                        NativeVehicleConfig otherConfig = vehicleConfigs[(int)(nextVehicle.id % vehicleConfigs.Length)];
                        dist -= math.max(0f, otherConfig.safeDistance - safeDistance);
                        if (dist < distanceToFront) distanceToFront = dist;
                    }

                } while (vehicleMap.TryGetNextValue(out nextIdx, ref nextIt));
            }

            // Double look-ahead for the edge after the next one to avoid entering an intersection if the way ahead is blocked
            if (nextEdge.connectionCount > 0 && (distanceToEnd + nextEdge.length) < lookAhead)
            {
                uint nextStableSeed = (uint)math.max(1, vehicle.id * 73856 + nextEdgeId * 19284);
                Random nextRandom = new Random(nextStableSeed);
                int nextNextEdgeOffset = nextRandom.NextInt(0, nextEdge.connectionCount);
                int nextNextEdgeId = connections[nextEdge.connectionStartIndex + nextNextEdgeOffset];

                if (vehicleMap.TryGetFirstValue(nextNextEdgeId, out int nextNextIdx, out NativeParallelMultiHashMapIterator<int> nextNextIt))
                {
                    do
                    {
                        NativeVehicle nextNextVehicle = previousStates[nextNextIdx];
                        float dist2 = distanceToEnd + nextEdge.length + nextNextVehicle.distance;
                        if (dist2 > 0)
                        {
                            NativeVehicleConfig otherConfig = vehicleConfigs[(int)(nextNextVehicle.id % vehicleConfigs.Length)];
                            dist2 -= math.max(0f, otherConfig.safeDistance - safeDistance);
                            if (dist2 < distanceToFront) distanceToFront = dist2;
                        }
                    } while (vehicleMap.TryGetNextValue(out nextNextIdx, ref nextNextIt));
                }
            }

            if (finalHasConflict)
            {
                canEnterIntersection = false;
                distanceToFront = math.min(distanceToFront, distanceToEnd);
            }
        }

        // Speed control logic
        float dynamicSafeDistance = safeDistance + vehicle.speed * 0.5f;
        if (distanceToFront < dynamicSafeDistance || !canEnterIntersection)
        {
            // Calculate EXACT required braking deceleration to reach 0 speed at exactly safeDistance
            float distToBrake = math.max(0.1f, distanceToFront - safeDistance);
            float requiredBrake = (vehicle.speed * vehicle.speed) / (2f * distToBrake);
            float actualBrake = math.max(acceleration * 2f, requiredBrake);
            
            vehicle.speed -= actualBrake * deltaTime;
            if (vehicle.speed < 0f) vehicle.speed = 0f;
        }
        else
        {
            vehicle.speed += acceleration * deltaTime;
            if (vehicle.speed > maxSpeed) vehicle.speed = maxSpeed;
        }
        vehicle.distance += vehicle.speed * deltaTime;

        // Check end of edge and decide next edge or stop if there aren't
        if (vehicle.distance >= currentEdge.length)
        {
            if (currentEdge.connectionCount > 0 && canEnterIntersection)
            {
                if (chosenNextEdgeId != -1)
                {
                    vehicle.currentEdgeId = chosenNextEdgeId;
                }
                else
                {
                    uint stableSeed = (uint)math.max(1, vehicle.id * 73856 + currentEdge.id * 19284);
                    Random random = new Random(stableSeed);
                    int nextEdgeOffset = random.NextInt(0, currentEdge.connectionCount);
                    vehicle.currentEdgeId = connections[currentEdge.connectionStartIndex + nextEdgeOffset];
                }
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