using UnityEngine;
using UnityEditor;
using UnityEngine.Splines;
using System.Collections.Generic;
using System.Linq;

// ── 3. VENTANA DEL EDITOR ──────────────────────────────
public class BuildingPlacerWindow : EditorWindow
{
    // Configuración de la herramienta
    private SplineContainer _targetSpline;
    private float _inset = 3.0f;
    private string _generatedTag = "Building";
    private float _cornerAngleThresh = 45f;

    // Control de Espacios
    private float _spacingFactor = 1.0f;
    private float _cornerGap = 6.0f; // NUEVO: Margen para que las esquinas no se solapen

    // Control de Límites
    private int _maxBuildingsPerType = 0;
    private int _maxConsecutiveSameType = 3;

    // Nuestro archivo de guardado y UI
    private BuildingPlacerProfile _profile;
    private Vector2 _scroll;
    private string _newProfileName = "MiNuevoBarrio";

    [MenuItem("Tools/Building Placer")]
    public static void ShowWindow()
    {
        GetWindow<BuildingPlacerWindow>("Building Placer");
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        GUILayout.Label("Configuración General", EditorStyles.boldLabel);
        _targetSpline = (SplineContainer)EditorGUILayout.ObjectField("City Block Spline", _targetSpline, typeof(SplineContainer), true);
        _inset = EditorGUILayout.FloatField("Inset (meters)", _inset);
        _cornerAngleThresh = EditorGUILayout.FloatField("Corner Angle", _cornerAngleThresh);

        EditorGUILayout.Space();
        GUILayout.Label("Ajuste de Espacios", EditorStyles.boldLabel);
        _spacingFactor = EditorGUILayout.Slider("Factor de Separación", _spacingFactor, 0.5f, 1.5f);
        _cornerGap = EditorGUILayout.FloatField("Hueco previo a Esquina (m)", _cornerGap);

        EditorGUILayout.Space();
        GUILayout.Label("Límites de Generación", EditorStyles.boldLabel);
        _maxBuildingsPerType = EditorGUILayout.IntField("Máx. Edificios por Tipo", _maxBuildingsPerType);
        _maxConsecutiveSameType = EditorGUILayout.IntSlider("Máx. Consecutivos Mismo Tipo", _maxConsecutiveSameType, 1, 10);

        EditorGUILayout.Space();
        GUILayout.Label("Datos de Perfil", EditorStyles.boldLabel);

        _profile = (BuildingPlacerProfile)EditorGUILayout.ObjectField("💾 Prefab Profile", _profile, typeof(BuildingPlacerProfile), false);

        if (_profile == null)
        {
            EditorGUILayout.HelpBox("Asigna un Perfil para ver las listas, o escribe un nombre abajo para crear uno nuevo.", MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            _newProfileName = EditorGUILayout.TextField("Nombre del Perfil", _newProfileName);
            if (GUILayout.Button("Crear y Asignar", GUILayout.Width(120)))
            {
                CreateCustomProfile(_newProfileName);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.Space();
        _profile.probDiffType = EditorGUILayout.Slider("Prob. Tipo Distinto", _profile.probDiffType, 0f, 1f);
        _profile.probSameTypeDiffSize = EditorGUILayout.Slider("Prob. Mismo Tipo / Dist. Tamaño", _profile.probSameTypeDiffSize, 0f, 1f);
        EditorGUILayout.Space();

        // ── LISTAS RESIDENCIALES ───────────────────────────
        DrawBuildingList("🏡 ResA Houses", _profile.resA);
        DrawBuildingList("🏡 ResB Houses", _profile.resB);
        DrawBuildingList("🏡 ResC Houses", _profile.resC);
        DrawBuildingList("🏡 ResD Houses", _profile.resD);

        // ── LISTA ESQUINAS ─────────────────────────────────
        GUILayout.Label("📐 Corner Prefabs:", EditorStyles.boldLabel);
        for (int i = 0; i < _profile.cornerPrefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            _profile.cornerPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(_profile.cornerPrefabs[i], typeof(GameObject), false);
            if (GUILayout.Button("X", GUILayout.Width(20))) { _profile.cornerPrefabs.RemoveAt(i); i--; }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Add corner prefab")) _profile.cornerPrefabs.Add(null);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(_profile);
        }

        EditorGUILayout.Space();

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("▶ Generate buildings", GUILayout.Height(30))) GenerateBuildings();

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("✕ Clear generated", GUILayout.Height(30))) ClearGenerated();

        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    private void CreateCustomProfile(string profileName)
    {
        if (string.IsNullOrEmpty(profileName)) return;
        BuildingPlacerProfile newProfile = ScriptableObject.CreateInstance<BuildingPlacerProfile>();
        string path = AssetDatabase.GenerateUniqueAssetPath($"Assets/{profileName}.asset");
        AssetDatabase.CreateAsset(newProfile, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        _profile = newProfile;
    }

    void DrawBuildingList(string title, List<BuildingData> list)
    {
        GUILayout.Label(title, EditorStyles.boldLabel);
        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            list[i].prefab = (GameObject)EditorGUILayout.ObjectField(list[i].prefab, typeof(GameObject), false);
            list[i].size = (BuildingSize)EditorGUILayout.EnumPopup(list[i].size, GUILayout.Width(80));
            if (GUILayout.Button("X", GUILayout.Width(20))) { list.RemoveAt(i); i--; }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Add to " + title.Replace("🏡 ", ""))) list.Add(new BuildingData());
        EditorGUILayout.Space();
    }

    void ClearGenerated()
    {
        var toDelete = GameObject.FindGameObjectsWithTag(_generatedTag);
        foreach (var go in toDelete) DestroyImmediate(go);
        Debug.Log($"Borrados {toDelete.Length} edificios generados.");
    }

    void GenerateBuildings()
    {
        if (_targetSpline == null) return;

        List<List<BuildingData>> allResLists = new List<List<BuildingData>> { _profile.resA, _profile.resB, _profile.resC, _profile.resD };
        if (!allResLists.Any(list => list.Any(b => b != null && b.prefab != null))) return;

        ClearGenerated();
        EnsureTag(_generatedTag);

        Spline spline = _targetSpline.Spline;
        int knotCount = spline.Count;

        int lastTypeIndex = -1;
        BuildingData lastBuilding = null;
        int[] typeCounters = new int[allResLists.Count];
        int consecutiveCount = 0;

        for (int i = 0; i < knotCount; i++)
        {
            int iNext = (i + 1) % knotCount;
            int iPrev = (i - 1 + knotCount) % knotCount;
            int iNextNext = (i + 2) % knotCount; // Nodo subsiguiente para mirar al futuro

            Vector3 pCurr = _targetSpline.transform.TransformPoint(spline[i].Position);
            Vector3 pNext = _targetSpline.transform.TransformPoint(spline[iNext].Position);
            Vector3 pPrev = _targetSpline.transform.TransformPoint(spline[iPrev].Position);
            Vector3 pNextNext = _targetSpline.transform.TransformPoint(spline[iNextNext].Position);

            Vector3 edgeDir = (pNext - pCurr).normalized;
            float edgeLen = Vector3.Distance(pCurr, pNext);

            Vector3 outward = new Vector3(-edgeDir.z, 0, edgeDir.x).normalized;
            Vector3 edgeMid = (pCurr + pNext) / 2f;
            Vector3 splineCenter = GetSplineCenter(spline, _targetSpline.transform);

            if (Vector3.Dot(outward, (splineCenter - edgeMid).normalized) > 0)
                outward = -outward;

            Quaternion rot = Quaternion.LookRotation(outward, Vector3.up);

            // 1. Detección de la esquina ACTUAL
            Vector3 edgeIn = (pCurr - pPrev).normalized;
            float angle = Vector3.Angle(edgeIn, edgeDir);
            bool isCorner = angle > _cornerAngleThresh;

            float cornerWidth = 0f;
            if (isCorner && _profile.cornerPrefabs.Count > 0)
            {
                var validCorners = _profile.cornerPrefabs.Where(c => c != null).ToList();
                if (validCorners.Count > 0)
                {
                    var prefab = validCorners[UnityEngine.Random.Range(0, validCorners.Count)];
                    Bounds b = GetPrefabBounds(prefab);
                    cornerWidth = b.size.x * _spacingFactor;

                    Vector3 pos = pCurr + edgeDir * (cornerWidth / 2f) + outward * (-(_inset + b.size.z / 2f));
                    pos.y = 0;
                    PlacePrefab(prefab, pos, rot);
                }
            }

            // 2. Detección de la FUTURA esquina (Look-ahead)
            Vector3 edgeDirNext = (pNextNext - pNext).normalized;
            bool nextIsCorner = Vector3.Angle(edgeDir, edgeDirNext) > _cornerAngleThresh;

            // Restamos el gap a la longitud disponible si nos acercamos a una esquina
            float effectiveEdgeLen = edgeLen;
            if (nextIsCorner)
            {
                effectiveEdgeLen -= _cornerGap;
            }

            // 3. Rellenar con edificios rectos
            float cursor = cornerWidth;
            Vector3 vStart = pCurr;

            while (true)
            {
                int prevTypeIndex = lastTypeIndex;

                BuildingData nextData = ChooseNextBuilding(allResLists, ref lastTypeIndex, lastBuilding, typeCounters, _maxBuildingsPerType, consecutiveCount, _maxConsecutiveSameType);
                if (nextData == null || nextData.prefab == null) break;

                Bounds b = GetPrefabBounds(nextData.prefab);
                float bw = b.size.x * _spacingFactor;
                float bd = b.size.z;

                // AHORA COMPROBAMOS CONTRA EL effectiveEdgeLen RECORTE
                if (cursor + bw > effectiveEdgeLen + 0.01f) break;

                // Solo si el edificio cabe (no ha hecho break), actualizamos los contadores
                if (lastTypeIndex == prevTypeIndex) consecutiveCount++;
                else consecutiveCount = 1;

                lastBuilding = nextData;
                typeCounters[lastTypeIndex]++;

                Vector3 pos = vStart + edgeDir * (cursor + bw / 2f) + outward * (-(_inset + bd / 2f));
                pos.y = 0;

                PlacePrefab(nextData.prefab, pos, rot);
                cursor += bw;
            }
        }

        Debug.Log("✅ Generación completada.");
    }

    // ── LÓGICA DE PROBABILIDAD ──────────────────────────────────
    BuildingData ChooseNextBuilding(List<List<BuildingData>> allLists, ref int lastTypeIndex, BuildingData lastBuilding, int[] typeCounters, int maxLimit, int consecutiveCount, int maxConsecutive)
    {
        List<int> validIndices = new List<int>();
        bool consecutiveLimitReached = (lastTypeIndex != -1 && consecutiveCount >= maxConsecutive);

        for (int i = 0; i < allLists.Count; i++)
        {
            if (allLists[i].Any(b => b != null && b.prefab != null))
            {
                if (maxLimit <= 0 || typeCounters[i] < maxLimit)
                {
                    if (consecutiveLimitReached && i == lastTypeIndex) continue;
                    validIndices.Add(i);
                }
            }
        }

        if (validIndices.Count == 0 && consecutiveLimitReached)
        {
            if (maxLimit <= 0 || typeCounters[lastTypeIndex] < maxLimit)
            {
                validIndices.Add(lastTypeIndex);
                consecutiveLimitReached = false;
            }
        }

        if (validIndices.Count == 0) return null;

        if (lastTypeIndex == -1 || lastBuilding == null || !validIndices.Contains(lastTypeIndex))
        {
            int rIdx = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
            var validBuildings = allLists[rIdx].Where(b => b != null && b.prefab != null).ToList();
            lastTypeIndex = rIdx;
            return validBuildings[UnityEngine.Random.Range(0, validBuildings.Count)];
        }

        float roll = UnityEngine.Random.value;
        int nextTypeIndex = lastTypeIndex;
        BuildingData nextBuilding = null;

        int currentTypeIndex = lastTypeIndex;
        BuildingSize currentSize = lastBuilding.size;

        if (consecutiveLimitReached || roll < _profile.probDiffType)
        {
            var otherIndices = validIndices.Where(i => i != currentTypeIndex).ToList();
            if (otherIndices.Count > 0)
            {
                nextTypeIndex = otherIndices[UnityEngine.Random.Range(0, otherIndices.Count)];
                var validBuildings = allLists[nextTypeIndex].Where(b => b != null && b.prefab != null).ToList();

                float subRoll = UnityEngine.Random.value;
                if (subRoll < 0.20f)
                {
                    var sameSize = validBuildings.Where(b => b.size == currentSize).ToList();
                    if (sameSize.Count > 0) nextBuilding = sameSize[UnityEngine.Random.Range(0, sameSize.Count)];
                }
                else
                {
                    var diffSize = validBuildings.Where(b => b.size != currentSize).ToList();
                    if (diffSize.Count > 0) nextBuilding = diffSize[UnityEngine.Random.Range(0, diffSize.Count)];
                }

                if (nextBuilding == null)
                    nextBuilding = validBuildings[UnityEngine.Random.Range(0, validBuildings.Count)];
            }
        }
        else if (roll < _profile.probDiffType + _profile.probSameTypeDiffSize)
        {
            var currentList = allLists[currentTypeIndex].Where(b => b != null && b.prefab != null).ToList();
            var diffSize = currentList.Where(b => b.size != currentSize).ToList();
            if (diffSize.Count > 0) nextBuilding = diffSize[UnityEngine.Random.Range(0, diffSize.Count)];
        }
        else
        {
            var currentList = allLists[currentTypeIndex].Where(b => b != null && b.prefab != null).ToList();
            var sameSize = currentList.Where(b => b.size == currentSize).ToList();
            if (sameSize.Count > 0) nextBuilding = sameSize[UnityEngine.Random.Range(0, sameSize.Count)];
        }

        if (nextBuilding == null)
        {
            nextTypeIndex = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
            var validBuildings = allLists[nextTypeIndex].Where(b => b != null && b.prefab != null).ToList();
            nextBuilding = validBuildings[UnityEngine.Random.Range(0, validBuildings.Count)];
        }

        lastTypeIndex = nextTypeIndex;
        return nextBuilding;
    }

    // ── HELPERS ────────────────────────────────────────────
    void PlacePrefab(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = pos;
        go.transform.rotation = rot * prefab.transform.rotation;
        go.tag = _generatedTag;
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