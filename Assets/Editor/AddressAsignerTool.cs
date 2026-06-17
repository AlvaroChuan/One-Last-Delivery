using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using TMPro;

public class AddressAssignerTool : EditorWindow
{
    private Transform _streetParent;
    private Transform _streetZeroPoint;
    private DropDownStringDatabase _dropdownDatabase;

    private string _targetStreetName = "North Boulevard";
    private int _startingBlockNumber = 100;
    private bool _isEvenSide = true;
    private float _blockGapThreshold = 25f;

    [MenuItem("Tools/City/Address Assigner")]
    public static void ShowWindow()
    {
        GetWindow<AddressAssignerTool>("Address Assigner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Global Settings", EditorStyles.boldLabel);
        _dropdownDatabase = (DropDownStringDatabase)EditorGUILayout.ObjectField("Street Database Asset", _dropdownDatabase, typeof(DropDownStringDatabase), false);

        EditorGUILayout.Space();

        GUILayout.Label("Scene References", EditorStyles.boldLabel);
        _streetParent = (Transform)EditorGUILayout.ObjectField("Sidewalk Parent", _streetParent, typeof(Transform), true);
        _streetZeroPoint = (Transform)EditorGUILayout.ObjectField("Zero Point (Start Location)", _streetZeroPoint, typeof(Transform), true);

        EditorGUILayout.Space();

        GUILayout.Label("Address Configuration", EditorStyles.boldLabel);
        _targetStreetName = EditorGUILayout.TextField("Street Name", _targetStreetName);
        _startingBlockNumber = EditorGUILayout.IntField("Starting Block (Hundreds)", _startingBlockNumber);
        _isEvenSide = EditorGUILayout.Toggle("Is Even Side?", _isEvenSide);
        _blockGapThreshold = EditorGUILayout.FloatField("Block Gap Threshold", _blockGapThreshold);

        EditorGUILayout.Space();

        if (GUILayout.Button("Assign Addresses Automatically", GUILayout.Height(35)))
        {
            AssignAddresses();
        }
    }

    private void AssignAddresses()
    {
        if (_streetParent == null || _streetZeroPoint == null)
        {
            Debug.LogError("Assigner: Faltan referencias de la escena (Sidewalk Parent o Zero Point).");
            return;
        }

        LocalAddressComponent[] doorsArray = _streetParent.GetComponentsInChildren<LocalAddressComponent>(true);

        if (doorsArray.Length == 0)
        {
            Debug.LogWarning("Assigner: No se encontraron portales en el padre seleccionado.");
            return;
        }

        RegisterStreetNameIfNeeded(_targetStreetName);

        List<LocalAddressComponent> unassignedDoors = doorsArray.ToList();
        List<LocalAddressComponent> orderedDoors = new List<LocalAddressComponent>();
        Vector3 currentReferencePoint = _streetZeroPoint.position;

        while (unassignedDoors.Count > 0)
        {
            LocalAddressComponent nextClosestDoor = unassignedDoors
                .OrderBy(d => Vector3.Distance(currentReferencePoint, d.transform.position))
                .First();

            orderedDoors.Add(nextClosestDoor);
            unassignedDoors.Remove(nextClosestDoor);
            currentReferencePoint = nextClosestDoor.transform.position;
        }

        int currentHundred = _startingBlockNumber;
        int indexInBlock = 0;

        Undo.RecordObjects(orderedDoors.ToArray(), "Assign Addresses");

        for (int i = 0; i < orderedDoors.Count; i++)
        {
            if (i > 0)
            {
                float gapToPreviousBuilding = Vector3.Distance(orderedDoors[i].transform.position, orderedDoors[i - 1].transform.position);

                if (gapToPreviousBuilding > _blockGapThreshold)
                {
                    currentHundred += 100;
                    indexInBlock = 0;
                }
            }

            int finalNumber = currentHundred + (indexInBlock * 2);
            if (!_isEvenSide)
            {
                finalNumber += 1;
            }

            SerializedObject so = new SerializedObject(orderedDoors[i]);
            SerializedProperty addressProp = so.FindProperty("_address");

            addressProp.FindPropertyRelative("streetName").stringValue = _targetStreetName;
            addressProp.FindPropertyRelative("number").intValue = finalNumber;

            so.ApplyModifiedProperties();

            TextMeshPro textMesh = orderedDoors[i].GetComponentInChildren<TextMeshPro>(true);

            if (textMesh != null)
            {
                Undo.RecordObject(textMesh, "Update Address Text");
                textMesh.text = finalNumber.ToString();
                EditorUtility.SetDirty(textMesh);
            }

            indexInBlock++;
        }

        Debug.Log($"<color=green><b>Success!</b></color> Portales y textos 3D actualizados correctamente en {_targetStreetName}.");
    }

    private void RegisterStreetNameIfNeeded(string newStreetName)
    {
        if (string.IsNullOrWhiteSpace(newStreetName)) return;

        if (_dropdownDatabase == null)
        {
            Debug.LogWarning("Assigner: ¡No has arrastrado tu base de datos al hueco 'Street Database Asset'!");
            return;
        }
        List<string> existingStreets = _dropdownDatabase.GetOptions("StreetNames");
        if (existingStreets.Contains(newStreetName))
        {
            return;
        }
        Undo.RecordObject(_dropdownDatabase, "Add Street Name");
        _dropdownDatabase.AddOption("StreetNames", newStreetName);
        EditorUtility.SetDirty(_dropdownDatabase);
        AssetDatabase.SaveAssets();
        Debug.Log($"<color=cyan><b>Database:</b></color> Calle '{newStreetName}' guardada con éxito en el Dropdown de forma nativa.");
    }
}