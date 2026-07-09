using System;
using UnityEngine;
using Mirror;

public class HashCatcher : MonoBehaviour
{
    void Start()
    {
        ushort targetId = 45578;
        Debug.Log($"Hunting for Message ID: {targetId}...");

        // Look through every assembly loaded in the project
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                // Check if the type implements NetworkMessage
                if (typeof(NetworkMessage).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    // Recreate Mirror's hashing logic
                    int hash = 23;
                    foreach (char c in type.FullName)
                    {
                        hash = hash * 31 + c;
                    }

                    // Convert to ushort just like Mirror does
                    ushort msgId = (ushort)(hash & 0xFFFF);

                    if (msgId == targetId)
                    {
                        Debug.LogError($"🎯 FOUND IT! ID {targetId} belongs to: {type.FullName}");
                        return; // Stop looking once we find it
                    }
                }
            }
        }
        Debug.Log("Finished searching. If it wasn't found, it might be a generated RPC/Command wrapper.");
    }
}