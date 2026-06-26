using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TerrainDetailManager : EditorWindow
{
    // Lista de terrenos
    public List<Terrain> terrains = new List<Terrain>();

    // Ajustes del componente Terrain
    public float detailDistance = 80f;
    public float detailDensity = 1.0f;

    // Ajustes del asset TerrainData
    public int detailResolution = 512;
    public int detailResolutionPerPatch = 32;

    private SerializedObject serializedObject;
    private SerializedProperty terrainsProperty;

    [MenuItem("Tools/Terrain Detail Manager")]
    public static void ShowWindow()
    {
        GetWindow<TerrainDetailManager>("Terrain Details");
    }

    private void OnEnable()
    {
        serializedObject = new SerializedObject(this);
        terrainsProperty = serializedObject.FindProperty("terrains");
    }

    private void OnGUI()
    {
        serializedObject.Update();

        GUILayout.Label("Gestión de Terrenos", EditorStyles.boldLabel);

        if (GUILayout.Button("Buscar todos los Terrenos en la Escena"))
        {
            terrains.Clear();
            terrains.AddRange(FindObjectsOfType<Terrain>());
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(terrainsProperty, true);
        EditorGUILayout.Space();

        // --- SECCIÓN 1: PROPIEDADES EN TIEMPO REAL ---
        GUILayout.Label("Ajustes de Renderizado (Componente)", EditorStyles.boldLabel);
        detailDistance = EditorGUILayout.Slider("Detail Distance", detailDistance, 0f, 1000f);
        detailDensity = EditorGUILayout.Slider("Detail Density", detailDensity, 0f, 1f);

        EditorGUILayout.Space();

        // --- SECCIÓN 2: PROPIEDADES DEL ASSET ---
        GUILayout.Label("Ajustes de Resolución (Terrain Data)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("CUIDADO: Cambiar la resolución modificará el archivo TerrainData. Unity remuestreará el mapa de detalles, lo que puede alterar ligeramente la posición de la hierba ya pintada. Asegúrate de guardar tu proyecto antes de aplicar cambios bruscos.", MessageType.Warning);

        detailResolution = EditorGUILayout.IntField("Detail Resolution", detailResolution);
        detailResolutionPerPatch = EditorGUILayout.IntField("Resolution Per Patch", detailResolutionPerPatch);

        EditorGUILayout.Space();

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
        if (GUILayout.Button("Aplicar Todos los Ajustes", GUILayout.Height(35)))
        {
            ApplySettings();
        }
        GUI.backgroundColor = Color.white;

        serializedObject.ApplyModifiedProperties();
    }

    private void ApplySettings()
    {
        int count = 0;
        foreach (Terrain t in terrains)
        {
            if (t != null)
            {
                // 1. Aplicamos los ajustes básicos al componente
                Undo.RecordObject(t, "Cambiar Renderizado Terreno");
                t.detailObjectDistance = detailDistance;
                t.detailObjectDensity = detailDensity;

                // 2. Aplicamos la resolución al TerrainData
                if (t.terrainData != null)
                {
                    Undo.RecordObject(t.terrainData, "Cambiar Resolución Detalles");

                    // La API nativa de Unity para cambiar ambas resoluciones de golpe
                    t.terrainData.SetDetailResolution(detailResolution, detailResolutionPerPatch);
                }

                count++;
            }
        }

        Debug.Log($"[Terrain Detail Manager] ¡Éxito! Actualizados {count} terrenos. Resolución fijada a {detailResolution} (Parches: {detailResolutionPerPatch}).");
    }
}