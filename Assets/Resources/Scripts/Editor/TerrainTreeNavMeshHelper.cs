using UnityEngine;
using UnityEditor;

public class TerrainTreeNavMeshHelper : MonoBehaviour
{
    [MenuItem("Tools/NavMesh/Generate Tree Colliders")]
    public static void GenerateColliders()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("No active Terrain found in the scene.");
            return;
        }

        // Delete previous colliders just in case to avoid duplicates
        DeleteColliders();

        GameObject tempParent = new GameObject("Temp_TreeColliders");

        foreach (TreeInstance tree in terrain.terrainData.treeInstances)
        {
            // Calculate the actual world position of the tree
            Vector3 worldPos = Vector3.Scale(tree.position, terrain.terrainData.size) + terrain.transform.position;

            // Create a primitive cylinder to act as the trunk collider
            GameObject col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            col.transform.position = worldPos;

            // Adjust the size (you can multiply tree.widthScale if your trunks are thicker)
            col.transform.localScale = new Vector3(tree.widthScale, tree.heightScale * 5f, tree.widthScale);

            col.transform.SetParent(tempParent.transform);

            // NavMeshSurface source collection handles inclusion; NavigationStatic is obsolete.
        }

        Debug.Log("Temporary colliders generated successfully! You can now Bake your NavMeshSurface.");
    }

    [MenuItem("Tools/NavMesh/Delete Tree Colliders")]
    public static void DeleteColliders()
    {
        GameObject temp = GameObject.Find("Temp_TreeColliders");
        if (temp != null)
        {
            DestroyImmediate(temp);
            Debug.Log("Temporary colliders deleted.");
        }
    }
}