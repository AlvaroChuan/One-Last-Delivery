using UnityEngine;
using UnityEditor;

public class LODSetupTool
{
    [MenuItem("Tools/Generar LOD Groups Automáticos (Street1)")]
    public static void SetupLODGroupsByString()
    {
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        int processedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Street1"))
            {
                if (obj.transform.parent != null && obj.transform.parent.name.Contains("Street1"))
                {
                    continue;
                }

                Renderer parentRenderer = obj.GetComponent<Renderer>();
                if (parentRenderer == null) continue;

                if (obj.transform.childCount == 0) continue;

                Transform childTransform = obj.transform.GetChild(0);
                Renderer childRenderer = childTransform.GetComponent<Renderer>();

                if (childRenderer == null) continue;

                Undo.RecordObject(obj, "Setup LOD Group");

                LODGroup lodGroup = obj.GetComponent<LODGroup>();
                if (lodGroup == null)
                {
                    lodGroup = Undo.AddComponent<LODGroup>(obj);
                }

                // Configuración de los niveles de LOD ajustada
                LOD[] lods = new LOD[2];

                // LOD 0: Malla del padre. Visible desde 100% hasta 16% de la pantalla.
                lods[0] = new LOD(0.16f, new Renderer[] { parentRenderer });

                // LOD 1: Malla del hijo. Visible desde 16% hasta 0% de la pantalla (Nunca hace culling).
                lods[1] = new LOD(0.0f, new Renderer[] { childRenderer });

                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();

                EditorUtility.SetDirty(obj);
                processedCount++;
            }
        }

        Debug.Log($"Proceso completado. Se han configurado los LODGroups en {processedCount} objetos. LOD1 al 16% y sin Culling.");
    }
}