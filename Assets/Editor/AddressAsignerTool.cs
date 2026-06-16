using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class AddressAssignerTool : EditorWindow
{
    // Tool variables
    private Transform streetParent;
    private Transform streetZeroPoint;
    private string targetStreetName = "North Boulevard";
    private int startingBlockNumber = 100;
    private bool isEvenSide = true;
    private float blockGapThreshold = 25f;

    // Creates the menu in the Unity top bar
    [MenuItem("Tools/City/Address Assigner")]
    public static void ShowWindow()
    {
        GetWindow<AddressAssignerTool>("Address Assigner");
    }

    // Draws the window interface
    private void OnGUI()
    {
        GUILayout.Label("Scene References", EditorStyles.boldLabel);
        streetParent = (Transform)EditorGUILayout.ObjectField("Sidewalk Parent", streetParent, typeof(Transform), true);

        // Changed label slightly to reflect it doesn't care about Z direction anymore
        streetZeroPoint = (Transform)EditorGUILayout.ObjectField("Zero Point (Start Location)", streetZeroPoint, typeof(Transform), true);

        EditorGUILayout.Space();

        GUILayout.Label("Address Configuration", EditorStyles.boldLabel);
        targetStreetName = EditorGUILayout.TextField("Street Name", targetStreetName);
        startingBlockNumber = EditorGUILayout.IntField("Starting Block (Hundreds)", startingBlockNumber);
        isEvenSide = EditorGUILayout.Toggle("Is Even Side?", isEvenSide);
        blockGapThreshold = EditorGUILayout.FloatField("Block Gap Threshold", blockGapThreshold);

        EditorGUILayout.Space();

        if (GUILayout.Button("Assign Addresses Automatically", GUILayout.Height(35)))
        {
            AssignAddresses();
        }
    }

    private void AssignAddresses()
    {
        if (streetParent == null || streetZeroPoint == null)
        {
            Debug.LogError("Assigner: You need to assign the Sidewalk Parent and the Zero Point.");
            return;
        }

        // 1. Find your specific component in all children
        LocalAddressComponent[] doors = streetParent.GetComponentsInChildren<LocalAddressComponent>(true);

        if (doors.Length == 0)
        {
            Debug.LogWarning("Assigner: No LocalAddressComponent found in the children.");
            return;
        }

        // 2. Register the street name if it's not in the DropdownStringDatabase
        RegisterStreetNameIfNeeded(targetStreetName);

        // 3. Sort spatially based on PURE DISTANCE from the zero point
        List<LocalAddressComponent> orderedDoors = doors
            .OrderBy(d => Vector3.Distance(streetZeroPoint.position, d.transform.position))
            .ToList();

        // 4. Iteration variables
        int currentHundred = startingBlockNumber;
        int indexInBlock = 0;
        float previousDistance = 0f;

        // Prepare the Undo system (Ctrl+Z)
        Undo.RecordObjects(orderedDoors.ToArray(), "Assign Addresses");

        for (int i = 0; i < orderedDoors.Count; i++)
        {
            // Calculate pure distance instead of forward projection
            float currentDistance = Vector3.Distance(streetZeroPoint.position, orderedDoors[i].transform.position);

            // Detect block jump (gap threshold)
            if (i > 0 && (currentDistance - previousDistance) > blockGapThreshold)
            {
                currentHundred += 100;
                indexInBlock = 0;
            }

            // Calculate final number
            int finalNumber = currentHundred + (indexInBlock * 2);
            if (!isEvenSide)
            {
                finalNumber += 1;
            }

            // 5. MODIFY COMPONENT (Using SerializedObject to bypass "private" fields)
            SerializedObject so = new SerializedObject(orderedDoors[i]);
            SerializedProperty addressProp = so.FindProperty("_address");

            // Assign values to the internal struct
            addressProp.FindPropertyRelative("streetName").stringValue = targetStreetName;
            addressProp.FindPropertyRelative("number").intValue = finalNumber;

            // Apply changes
            so.ApplyModifiedProperties();

            // Save state for the next building
            previousDistance = currentDistance;
            indexInBlock++;
        }

        Debug.Log($"<color=green><b>Success!</b></color> Sorted and assigned {orderedDoors.Count} doors on {targetStreetName} based on pure distance.");
    }

    // --- STREET REGISTRATION LOGIC ---
    private void RegisterStreetNameIfNeeded(string newStreetName)
    {
        if (string.IsNullOrWhiteSpace(newStreetName)) return;

        // 1. Automatically find the DropdownStringDatabase file in the entire project
        string[] guids = AssetDatabase.FindAssets("t:DropdownStringDatabase");
        if (guids.Length == 0)
        {
            Debug.LogWarning("Assigner: No 'DropdownStringDatabase' found in the project.");
            return;
        }

        // Load the first found asset
        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        ScriptableObject database = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

        if (database != null)
        {
            SerializedObject so = new SerializedObject(database);

            // Unity capitalizes names in the Inspector. We look for the actual variable name in code.
            SerializedProperty entriesProp = so.FindProperty("Entries") ?? so.FindProperty("entries") ?? so.FindProperty("_entries");

            if (entriesProp == null)
            {
                Debug.LogWarning("Assigner: Could not find the 'Entries' list in the database.");
                return;
            }

            // 2. Loop through the entries to find the "StreetNames" Key
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(i);
                SerializedProperty keyProp = entryProp.FindPropertyRelative("Key") ?? entryProp.FindPropertyRelative("key");

                if (keyProp != null && keyProp.stringValue == "StreetNames")
                {
                    SerializedProperty optionsProp = entryProp.FindPropertyRelative("Options") ?? entryProp.FindPropertyRelative("options");

                    if (optionsProp != null)
                    {
                        // 3. Check if the street already exists in the options list
                        bool exists = false;
                        for (int j = 0; j < optionsProp.arraySize; j++)
                        {
                            if (optionsProp.GetArrayElementAtIndex(j).stringValue == newStreetName)
                            {
                                exists = true;
                                break;
                            }
                        }

                        // 4. If it doesn't exist, append it
                        if (!exists)
                        {
                            int newIndex = optionsProp.arraySize;
                            optionsProp.InsertArrayElementAtIndex(newIndex);
                            optionsProp.GetArrayElementAtIndex(newIndex).stringValue = newStreetName;

                            so.ApplyModifiedProperties(); // Apply changes to SerializedObject

                            EditorUtility.SetDirty(database); // Mark the file as modified
                            AssetDatabase.SaveAssets();       // Save the asset to disk

                            Debug.Log($"<color=cyan><b>Database:</b></color> Street '{newStreetName}' automatically added to the Dropdown.");
                        }
                    }
                    break; // Found "StreetNames", no need to check other keys
                }
            }
        }
    }
}