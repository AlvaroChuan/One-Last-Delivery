using System.Collections.Generic;
using UnityEngine;

public class PackageDistanceDetector : MonoBehaviour
{
    PackageSpawner _packageSpawner;

    void Awake()
    {
        _packageSpawner = FindAnyObjectByType<PackageSpawner>();
    }
    public GameObject DetectClosestNonCorruptedPackage(float detectionRadius, HashSet<GameObject> excludedPackages = null)
    {
        List<GameObject> packages = _packageSpawner?.SpawnedPackages;
        List<bool> corruptedPackages = _packageSpawner?.CorruptedPackages;
        if (packages == null || packages.Count == 0)
            return null;

        GameObject closestPackage = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < packages.Count; i++)
        {
            var package = packages[i];
            if (package == null) continue; // Skip if the package reference is null

            // Check if the package is corrupted
            bool isCorrupted = (corruptedPackages != null && i < corruptedPackages.Count) ? corruptedPackages[i] : false;
            if (isCorrupted)
            {
                continue; // Skip corrupted packages
            }

            // Check if the package is in the excluded list
            if (excludedPackages != null && excludedPackages.Contains(package))
            {
                continue; // Skip excluded packages
            }

            Vector3 packagePosition = package.transform.position;
            Vector3 detectorPosition = transform.position;
            packagePosition.y = 0f; // Ignore vertical distance
            detectorPosition.y = 0f; // Ignore vertical distance

            float distance = Vector3.Distance(detectorPosition, packagePosition);
            if (distance < closestDistance && distance <= detectionRadius)
            {
                closestDistance = distance;
                closestPackage = package;
            }
        }

        return closestPackage;
    }
}