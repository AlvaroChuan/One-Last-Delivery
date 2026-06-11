using UnityEngine;
using System.Collections.Generic;
using System;

// ── 1. DATOS SERIALIZABLES ─────────────────────────────
public enum BuildingSize { High, Mid, Low, VeryLow }

[Serializable]
public class BuildingData
{
    public GameObject prefab;
    public BuildingSize size = BuildingSize.Mid;
}

// ── 2. PERFIL DE GUARDADO (SCRIPTABLE OBJECT) ──────────
[CreateAssetMenu(fileName = "NewCityBlockProfile", menuName = "Tools/Building Placer Profile")]
public class BuildingPlacerProfile : ScriptableObject
{
    public List<BuildingData> resA = new List<BuildingData>();
    public List<BuildingData> resB = new List<BuildingData>();
    public List<BuildingData> resC = new List<BuildingData>();
    public List<BuildingData> resD = new List<BuildingData>();

    public List<GameObject> cornerPrefabs = new List<GameObject>();

    public float probDiffType = 0.60f;
    public float probSameTypeDiffSize = 0.30f;
}