using System;
using System.Reflection;
using UnityEngine;

public class HashFinder : MonoBehaviour
{
    public ushort targetId = 45578;

    void Start()
    {
        Debug.Log($"Searching all scripts for Mirror Message ID: {targetId}...");

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (string.IsNullOrEmpty(type.FullName)) continue;

                // Run the exact hash algorithm Mirror uses
                ushort generatedId = GetMirrorHash(type.FullName);

                if (generatedId == targetId)
                {
                    Debug.LogError($"🎯 FOUND IT! The missing message type is: {type.FullName}");
                    return;
                }
            }
        }

        Debug.LogWarning("Search finished. If nothing was found, check if you have uncompiled script errors.");
    }

    // Mirror's exact internal algorithm for generating 16-bit message IDs
    ushort GetMirrorHash(string name)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in name)
            {
                hash = hash * 31 + c;
            }
            return (ushort)(hash & 0xFFFF);
        }
    }
}