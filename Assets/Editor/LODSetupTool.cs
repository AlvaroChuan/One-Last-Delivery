using UnityEngine;
using UnityEditor;

public class LODSetupTool
{
    [MenuItem("Tools/Generar LOD Groups (Self + Cull)")]
    public static void SetupLODGroupsSimple()
    {
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        int processedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            // Filtro por nombre
            if (obj.name.Contains("lamp"))
            {
                Renderer renderer = obj.GetComponent<Renderer>();

                // Si el objeto no tiene Renderer, no tiene sentido ponerle LODGroup
                if (renderer == null) continue;

                Undo.RecordObject(obj, "Setup LOD Group Simple");

                LODGroup lodGroup = obj.GetComponent<LODGroup>();
                if (lodGroup == null)
                {
                    lodGroup = Undo.AddComponent<LODGroup>(obj);
                }

                // Definimos los dos niveles:
                // LOD 0: El objeto mismo (visible hasta el 16%)
                // LOD 1: Vacío (culling)
                LOD[] lods = new LOD[2];

                lods[0] = new LOD(0.16f, new Renderer[] { renderer });
                lods[1] = new LOD(0.0f, new Renderer[] { }); // Array vacío = Culling

                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();

                EditorUtility.SetDirty(obj);
                processedCount++;
            }
        }

        Debug.Log($"Configuración completada: {processedCount} objetos configurados con LOD0 propio y Culling.");
    }
}