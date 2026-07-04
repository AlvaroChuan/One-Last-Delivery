using UnityEditor;
using UnityEngine;

public class SpeedCameraPlacer
{
    [MenuItem("Tools/Place Speed Cameras in Scene")]
    public static void PlaceSpeedCamerasInScene()
    {
        GameObject speedCameraPrefab = Resources.Load<GameObject>("Prefabs/City/SpeedCamera");
        GameObject speedCameraParent = GameObject.Find("CitySpeedCameras");
        if (speedCameraPrefab == null)
        {
            Debug.LogError("Speed camera prefab not found at path: " + "Prefabs/City/SpeedCamera");
            return;
        }

        Undo.SetCurrentGroupName("Place Speed Cameras");
        int group = Undo.GetCurrentGroup();

        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("SpeedSign"))
            {
                GameObject speedCameraInstance = PrefabUtility.InstantiatePrefab(speedCameraPrefab) as GameObject;
                if (speedCameraInstance != null)
                {
                    Undo.RegisterCreatedObjectUndo(speedCameraInstance, "Place Speed Camera");

                    speedCameraInstance.transform.position = obj.transform.position;
                    speedCameraInstance.transform.position = new Vector3(speedCameraInstance.transform.position.x, 5.615543f, speedCameraInstance.transform.position.z);
                    speedCameraInstance.transform.SetParent(speedCameraParent.transform);

                    MeshRenderer signRenderer = obj.GetComponent<MeshRenderer>();

                    float speedLimit = 25f;

                    foreach (Material mat in signRenderer.sharedMaterials)
                    {
                        if (mat.name.Contains("25"))
                        {
                            DevLogger.Log($"Found speed sign with material: {mat.name}, setting speed limit to 25");
                            speedLimit = 25f;
                            break;
                        }
                        else if (mat.name.Contains("35"))
                        {
                            speedLimit = 35f;
                            break;
                        }
                        else if (mat.name.Contains("45"))
                        {
                            speedLimit = 45f;
                            break;
                        }
                        else if (mat.name.Contains("65"))
                        {
                            speedLimit = 65f;
                            break;
                        }
                    }

                    SpeedCamera speedCameraScript = speedCameraInstance.GetComponent<SpeedCamera>();
                    speedCameraScript.SetSpeedLimit(speedLimit);
                }
                else
                {
                    Debug.LogError("Failed to instantiate speed camera prefab.");
                }
            }
        }

        Undo.CollapseUndoOperations(group);
    }
}
