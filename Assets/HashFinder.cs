using Mirror;
using UnityEngine;


public class HashFinder : MonoBehaviour
{
    void Start()
    {
        DevLogger.Log("Hash for TrafficBatchMessage: " + typeof(TrafficBatchMessage).FullName.GetStableHashCode());
        DevLogger.Log("Hash for ServerCarCrashCarMessage: " + typeof(ServerCarCrashCarMessage).FullName.GetStableHashCode());

    }
}