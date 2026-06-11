using UnityEngine;
using UnityEditor;
using UnityEditor.Splines;
using UnityEngine.Splines;
using System.Collections.Generic;

public class BuildingPlacerWindow : EditorWindow
{
    // ── CONFIGURACIÓN ──────────────────────────────────────
    private SplineContainer targetSpline;
    private float inset = 3.0f;
    private string generatedTag = "Building";

    // Prefabs separados por tipo
    private List<GameObject> straightPrefabs = new List<GameObject>();
    private List<GameObject> cornerPrefabs = new List<GameObject>();
    private float cornerAngleThresh = 45f;

    private Vector2 scroll;

    [MenuItem("Tools/Building Placer")]
    public static void ShowWindow()
    {
        GetWindow<BuildingPlacerWindow>("Building Placer");
    }

    void OnGUI()
    {
        GUILayout.Label("Building Placer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Spline objetivo
        targetSpline = (SplineContainer)EditorGUILayout.ObjectField(
            "Spline de manzana", targetSpline, typeof(SplineContainer), true);

        inset = EditorGUILayout.FloatField("Inset (metros)", inset);
        cornerAngleThresh = EditorGUILayout.FloatField("Ángulo esquina", cornerAngleThresh);

        EditorGUILayout.Space();

        // Lista de prefabs rectos
        GUILayout.Label("Prefabs tramos rectos:", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(150));
        for (int i = 0; i < straightPrefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            straightPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                straightPrefabs[i], typeof(GameObject), false);
            if (GUILayout.Button("X", GUILayout.Width(20)))
                straightPrefabs.RemoveAt(i);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        if (GUILayout.Button("+ Añadir prefab recto"))
            straightPrefabs.Add(null);

        EditorGUILayout.Space();

        // Lista de prefabs de esquina
        GUILayout.Label("Prefabs esquinas:", EditorStyles.boldLabel);
        for (int i = 0; i < cornerPrefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            cornerPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                cornerPrefabs[i], typeof(GameObject), false);
            if (GUILayout.Button("X", GUILayout.Width(20)))
                cornerPrefabs.RemoveAt(i);
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Añadir prefab esquina"))
            cornerPrefabs.Add(null);

        EditorGUILayout.Space();

        // Botones
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("▶ Generar edificios"))
            GenerateBuildings();

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("✕ Borrar generados"))
            ClearGenerated();

        GUI.backgroundColor = Color.white;
    }

    // ── BORRAR GENERADOS ───────────────────────────────────
    void ClearGenerated()
    {
        var toDelete = GameObject.FindGameObjectsWithTag(generatedTag);
        foreach (var go in toDelete)
            DestroyImmediate(go);
        Debug.Log($"Borrados {toDelete.Length} edificios generados");
    }

    // ── GENERAR ────────────────────────────────────────────
    void GenerateBuildings()
    {
        if (targetSpline == null)
        {
            Debug.LogError("Asigna un SplineContainer primero");
            return;
        }
        if (straightPrefabs.Count == 0)
        {
            Debug.LogError("Añade al menos un prefab de tramo recto");
            return;
        }

        ClearGenerated();

        // Asegurarse de que el tag existe
        EnsureTag(generatedTag);

        Spline spline = targetSpline.Spline;
        int knotCount = spline.Count;

        for (int i = 0; i < knotCount; i++)
        {
            int iNext = (i + 1) % knotCount;
            int iPrev = (i - 1 + knotCount) % knotCount;

            // Posiciones de los knots en espacio mundo
            Vector3 pCurr = targetSpline.transform.TransformPoint(spline[i].Position);
            Vector3 pNext = targetSpline.transform.TransformPoint(spline[iNext].Position);
            Vector3 pPrev = targetSpline.transform.TransformPoint(spline[iPrev].Position);

            Vector3 edgeDir = (pNext - pCurr).normalized;
            float edgeLen = Vector3.Distance(pCurr, pNext);

            // Normal hacia afuera (perpendicular en XZ)
            Vector3 outward = new Vector3(-edgeDir.z, 0, edgeDir.x).normalized;

            // Verificar que apunta hacia afuera del spline
            Vector3 edgeMid = (pCurr + pNext) / 2f;
            Vector3 splineCenter = GetSplineCenter(spline, targetSpline.transform);
            if (Vector3.Dot(outward, (splineCenter - edgeMid).normalized) > 0)
                outward = -outward;

            // Rotación mirando hacia la calle
            Quaternion rot = Quaternion.LookRotation(outward, Vector3.up);

            // ── Detectar esquina ───────────────────────────
            Vector3 edgeIn = (pCurr - pPrev).normalized;
            float angle = Vector3.Angle(edgeIn, edgeDir);
            bool isCorner = angle > cornerAngleThresh;

            float cornerWidth = 0f;
            if (isCorner && cornerPrefabs.Count > 0)
            {
                var prefab = cornerPrefabs[Random.Range(0, cornerPrefabs.Count)];
                if (prefab != null)
                {
                    Bounds b = GetPrefabBounds(prefab);
                    cornerWidth = b.size.x;

                    Vector3 pos = pCurr
                        + edgeDir * (cornerWidth / 2f)
                        + outward * (-(inset + b.size.z / 2f));
                    pos.y = 0;

                    PlacePrefab(prefab, pos, rot);
                }
            }

            // ── Tramo recto ────────────────────────────────
            float cursor = cornerWidth;
            Vector3 vStart = pCurr;

            while (true)
            {
                var prefab = straightPrefabs[Random.Range(0, straightPrefabs.Count)];
                if (prefab == null) break;

                Bounds b = GetPrefabBounds(prefab);
                float bw = b.size.x;  // ancho paralelo al borde
                float bd = b.size.z;  // profundidad

                if (cursor + bw > edgeLen + 0.01f) break;

                Vector3 pos = vStart
                    + edgeDir * (cursor + bw / 2f)
                    + outward * (-(inset + bd / 2f));
                pos.y = 0;

                PlacePrefab(prefab, pos, rot);
                cursor += bw;
            }
        }

        Debug.Log("✅ Edificios generados correctamente");
    }

    // ── HELPERS ────────────────────────────────────────────
    void PlacePrefab(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = pos;
        go.transform.rotation = rot * prefab.transform.rotation;
        go.tag = generatedTag;
        Undo.RegisterCreatedObjectUndo(go, "Place Building");
    }

    Bounds GetPrefabBounds(GameObject prefab)
    {
        var renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one * 8f);
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    Vector3 GetSplineCenter(Spline spline, Transform t)
    {
        Vector3 sum = Vector3.zero;
        foreach (var knot in spline) sum += t.TransformPoint(knot.Position);
        return sum / spline.Count;
    }

    void EnsureTag(string tag)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        for (int i = 0; i < tagsProp.arraySize; i++)
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }
}