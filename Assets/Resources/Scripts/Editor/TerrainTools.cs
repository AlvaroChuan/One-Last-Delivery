using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TerrainTreeTools
{
    // Creates a new option in the top menu of Unity
    [MenuItem("Tools/Terrain/Add Selected Prefabs As Trees")]
    static void AddSelectedPrefabsAsTrees()
    {
        // Find the active Terrain in the current scene
        Terrain terrain = Terrain.activeTerrain;
        
        if (terrain == null)
        {
            Debug.LogError("No active Terrain found in the scene. Please ensure there is one.");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        List<TreePrototype> treePrototypes = new List<TreePrototype>(terrainData.treePrototypes);
        int addedTrees = 0;

        // Loop through all currently selected objects in the editor
        foreach (Object obj in Selection.objects)
        {
            GameObject go = obj as GameObject;
            
            // Verify that it is a GameObject and a valid Prefab in the project
            if (go != null && PrefabUtility.IsPartOfPrefabAsset(go))
            {
                // Prevent adding duplicates if they are already in the Terrain
                bool exists = treePrototypes.Exists(t => t.prefab == go);
                if (!exists)
                {
                    TreePrototype newTree = new TreePrototype();
                    newTree.prefab = go;
                    treePrototypes.Add(newTree);
                    addedTrees++;
                }
            }
        }

        // If valid trees were found, apply them to the Terrain
        if (addedTrees > 0)
        {
            terrainData.treePrototypes = treePrototypes.ToArray();
            terrain.Flush(); // Update the Terrain in the editor
            Debug.Log($"Success! {addedTrees} trees have been added to the Terrain.");
        }
        else
        {
            Debug.LogWarning("No trees were added. Make sure to select prefabs in the Project window (not in the Hierarchy).");
        }
    }
}