using UnityEngine;
using UnityEditor;
using UnityEngine.Splines;
using System.Collections.Generic;
using System.Linq;

// ── 3. EDITOR WINDOW ──────────────────────────────
public class BuildingPlacerWindow : EditorWindow
{
    // Tool configuration
    private SplineContainer _targetSpline;
    private float _inset = 3.0f;
    private string _generatedTag = "Building";
    private float _cornerAngleThresh = 45f;

    // Simplified space control
    private float _spacingFactor = 1.0f;
    private float _cornerGap = 6.0f; // Space left free for the "Filler" logic

    // Generation limits
    private int _maxBuildingsPerType = 0;
    private int _maxConsecutiveSameType = 3;

    // Save profile and UI
    private BuildingPlacerProfile _profile;
    private Vector2 _scroll;
    private string _newProfileName = "MyNewNeighborhood";

    [MenuItem("Tools/Building Placer")]
    public static void ShowWindow()
    {
        GetWindow<BuildingPlacerWindow>("Building Placer");
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        GUILayout.Label("General Configuration", EditorStyles.boldLabel);
        _targetSpline = (SplineContainer)EditorGUILayout.ObjectField("City Block Spline", _targetSpline, typeof(SplineContainer), true);
        _inset = EditorGUILayout.FloatField("Inset (meters)", _inset);
        _cornerAngleThresh = EditorGUILayout.FloatField("Corner Angle", _cornerAngleThresh);

        EditorGUILayout.Space();
        GUILayout.Label("Space Adjustment", EditorStyles.boldLabel);
        _spacingFactor = EditorGUILayout.Slider("Straight Spacing Factor", _spacingFactor, 0.5f, 1.5f);
        _cornerGap = EditorGUILayout.FloatField("Corner Gap (m)", _cornerGap);

        EditorGUILayout.Space();
        GUILayout.Label("Generation Limits", EditorStyles.boldLabel);
        _maxBuildingsPerType = EditorGUILayout.IntField("Max Buildings Per Type", _maxBuildingsPerType);
        _maxConsecutiveSameType = EditorGUILayout.IntSlider("Max Consecutive Same Type", _maxConsecutiveSameType, 1, 10);

        EditorGUILayout.Space();
        GUILayout.Label("Profile Data", EditorStyles.boldLabel);

        _profile = (BuildingPlacerProfile)EditorGUILayout.ObjectField("Prefab Profile", _profile, typeof(BuildingPlacerProfile), false);

        if (_profile == null)
        {
            EditorGUILayout.HelpBox("Assign a Profile to view lists, or type a name below to create a new one.", MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            _newProfileName = EditorGUILayout.TextField("Profile Name", _newProfileName);
            if (GUILayout.Button("Create & Assign", GUILayout.Width(120)))
            {
                CreateCustomProfile(_newProfileName);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.Space();
        _profile.probDiffType = EditorGUILayout.Slider("Prob. Different Type", _profile.probDiffType, 0f, 1f);
        _profile.probSameTypeDiffSize = EditorGUILayout.Slider("Prob. Same Type / Diff. Size", _profile.probSameTypeDiffSize, 0f, 1f);
        EditorGUILayout.Space();

        // ── RESIDENTIAL LISTS ───────────────────────────
        DrawBuildingList("ResA Houses", _profile.resA);
        DrawBuildingList("ResB Houses", _profile.resB);
        DrawBuildingList("ResC Houses", _profile.resC);
        DrawBuildingList("ResD Houses", _profile.resD);

        // ── CORNER LIST ─────────────────────────────────
        GUILayout.Label("Corner Prefabs:", EditorStyles.boldLabel);
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
        if (GUILayout.Button("Generate buildings", GUILayout.Height(30))) GenerateBuildings();

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Clear generated", GUILayout.Height(30))) ClearGenerated();

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
        if (GUILayout.Button("+ Add to " + title.Replace(" Houses", ""))) list.Add(new BuildingData());
        EditorGUILayout.Space();
    }

    void ClearGenerated()
    {
        var toDelete = GameObject.FindGameObjectsWithTag(_generatedTag);
        foreach (var go in toDelete) DestroyImmediate(go);
        Debug.Log($"Cleared {toDelete.Length} generated buildings.");
    }

    // ── CORE GENERATION ENGINE WITH SCALED CAPPING ──
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
            int iNextNext = (i + 2) % knotCount;

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

            // 1. Current corner detection
            Vector3 edgeIn = (pCurr - pPrev).normalized;
            bool isCorner = Vector3.Angle(edgeIn, edgeDir) > _cornerAngleThresh;

            float cornerWidth = 0f;
            if (isCorner && _profile.cornerPrefabs.Count > 0)
            {
                var validCorners = _profile.cornerPrefabs.Where(c => c != null).ToList();
                if (validCorners.Count > 0)
                {
                    var prefab = validCorners[UnityEngine.Random.Range(0, validCorners.Count)];
                    Bounds b = GetPrefabBounds(prefab);
                    cornerWidth = b.size.x;

                    Vector3 pos = pCurr + edgeDir * (cornerWidth / 2f) + outward * (-(_inset + b.size.z / 2f));
                    pos.y = 0;
                    PlacePrefab(prefab, pos, rot);
                }
            }

            // 2. Future corner detection (Look-ahead)
            Vector3 edgeDirNext = (pNextNext - pNext).normalized;
            bool nextIsCorner = Vector3.Angle(edgeDir, edgeDirNext) > _cornerAngleThresh;

            float nextCornerDepth = 0f;
            if (nextIsCorner && _profile.cornerPrefabs.Count > 0)
            {
                var validCorners = _profile.cornerPrefabs.Where(c => c != null).ToList();
                if (validCorners.Count > 0)
                {
                    nextCornerDepth = GetPrefabBounds(validCorners[0]).size.z;
                }
            }

            // Define street core
            float coreStart = cornerWidth > 0f ? cornerWidth + _cornerGap : 0f;
            float coreEnd = edgeLen;
            if (nextIsCorner) coreEnd -= (nextCornerDepth + _cornerGap);

            if (coreStart > coreEnd)
            {
                coreStart = Mathf.Max(cornerWidth, edgeLen - nextCornerDepth);
                coreStart = Mathf.Min(coreStart, edgeLen);
                coreEnd = coreStart;
            }

            // 3. Fill central core with straight buildings
            float cursor = coreStart;
            Vector3 vStart = pCurr;

            while (true)
            {
                int prevTypeIndex = lastTypeIndex;
                float remainingSpace = coreEnd - cursor;

                BuildingData nextData = ChooseNextBuilding(allResLists, ref lastTypeIndex, lastBuilding, typeCounters, _maxBuildingsPerType, consecutiveCount, _maxConsecutiveSameType, remainingSpace);
                if (nextData == null || nextData.prefab == null) break;

                Bounds b = GetPrefabBounds(nextData.prefab);

                float bw = b.size.x * _spacingFactor;
                float bd = b.size.z;

                if (cursor + bw > coreEnd + 0.01f) break;

                if (lastTypeIndex == prevTypeIndex) consecutiveCount++;
                else consecutiveCount = 1;

                lastBuilding = nextData;
                typeCounters[lastTypeIndex]++;

                Vector3 pos = vStart + edgeDir * (cursor + bw / 2f) + outward * (-(_inset + bd / 2f));
                pos.y = 0;

                PlacePrefab(nextData.prefab, pos, rot);
                cursor += bw;
            }

            float actualCoreEnd = cursor;

            // ── 4. INTELLIGENT GAP SEALING WITH UNIFORM/CAPPED SCALING ──

            // START Gap (Post-Current Corner)
            if (isCorner && coreStart > cornerWidth)
            {
                float gapStart = cornerWidth;
                float gapSize = coreStart - gapStart;

                if (gapSize > 0.1f)
                {
                    float origWidth;
                    BuildingData filler = FindBestBuildingForGap(allResLists, gapSize, out origWidth);
                    if (filler != null)
                    {
                        float scaleFactor = gapSize / origWidth;
                        float zScale = Mathf.Max(scaleFactor, 0.8f); // Minimum Z scale cap

                        float posCursor = gapStart + gapSize / 2f;
                        float scaledDepth = GetPrefabBounds(filler.prefab).size.z * zScale;

                        Vector3 pos = vStart + edgeDir * posCursor + outward * (-(_inset + scaledDepth / 2f));
                        pos.y = 0;
                        PlaceScaledPrefab(filler.prefab, pos, rot, scaleFactor, zScale);
                    }
                }
            }

            // END Gap (Pre-Next Corner)
            if (nextIsCorner && (edgeLen - nextCornerDepth) > actualCoreEnd)
            {
                float gapStart = actualCoreEnd;
                float gapEnd = edgeLen - nextCornerDepth;
                float gapSize = gapEnd - gapStart;

                if (gapSize > 0.1f)
                {
                    float origWidth;
                    BuildingData filler = FindBestBuildingForGap(allResLists, gapSize, out origWidth);
                    if (filler != null)
                    {
                        float scaleFactor = gapSize / origWidth;
                        float zScale = Mathf.Max(scaleFactor, 0.8f); // Minimum Z scale cap

                        float posCursor = gapStart + gapSize / 2f;
                        float scaledDepth = GetPrefabBounds(filler.prefab).size.z * zScale;

                        Vector3 pos = vStart + edgeDir * posCursor + outward * (-(_inset + scaledDepth / 2f));
                        pos.y = 0;
                        PlaceScaledPrefab(filler.prefab, pos, rot, scaleFactor, zScale);
                    }
                }
            }
        }

        Debug.Log("Generation Completed: Base blocks + Scaled corner caps.");
    }

    // ── SEARCH FOR THE BEST FITTING BUILDING ──
    BuildingData FindBestBuildingForGap(List<List<BuildingData>> allLists, float targetGap, out float originalWidth)
    {
        BuildingData best = null;
        float closestDiff = float.MaxValue;
        originalWidth = 1f;

        foreach (var list in allLists)
        {
            foreach (var b in list)
            {
                if (b != null && b.prefab != null)
                {
                    float bw = GetPrefabBounds(b.prefab).size.x;
                    float diff = Mathf.Abs(bw - targetGap);
                    if (diff < closestDiff)
                    {
                        closestDiff = diff;
                        best = b;
                        originalWidth = bw;
                    }
                }
            }
        }
        return best;
    }

    // ── PREFAB PLACEMENT WITH INDEPENDENT Z SCALING ──
    void PlaceScaledPrefab(GameObject prefab, Vector3 pos, Quaternion rot, float xyScale, float zScale)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = pos;
        go.transform.rotation = rot * prefab.transform.rotation;

        Vector3 newScale = go.transform.localScale;
        newScale.x *= xyScale;
        newScale.y *= xyScale;
        newScale.z *= zScale; // Applies the cap limit of 0.8 min
        go.transform.localScale = newScale;

        go.tag = _generatedTag;
        Undo.RegisterCreatedObjectUndo(go, "Place Scaled Building");
    }

    // ── STRAIGHT PROBABILITY LOGIC ──────────────────────────────
    BuildingData ChooseNextBuilding(List<List<BuildingData>> allLists, ref int lastTypeIndex, BuildingData lastBuilding, int[] typeCounters, int maxLimit, int consecutiveCount, int maxConsecutive, float remainingSpace)
    {
        List<List<BuildingData>> fittingLists = new List<List<BuildingData>>();
        for (int i = 0; i < allLists.Count; i++)
        {
            var validInList = new List<BuildingData>();
            foreach (var b in allLists[i])
            {
                if (b != null && b.prefab != null)
                {
                    float bw = GetPrefabBounds(b.prefab).size.x * _spacingFactor;
                    if (bw <= remainingSpace + 0.05f) validInList.Add(b);
                }
            }
            fittingLists.Add(validInList);
        }

        List<int> validIndices = new List<int>();
        bool consecutiveLimitReached = (lastTypeIndex != -1 && consecutiveCount >= maxConsecutive);

        for (int i = 0; i < fittingLists.Count; i++)
        {
            if (fittingLists[i].Count > 0)
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
                if (fittingLists[lastTypeIndex].Count > 0)
                {
                    validIndices.Add(lastTypeIndex);
                    consecutiveLimitReached = false;
                }
            }
        }

        if (validIndices.Count == 0) return null;

        if (lastTypeIndex == -1 || lastBuilding == null || !validIndices.Contains(lastTypeIndex))
        {
            int rIdx = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
            var validBuildings = fittingLists[rIdx];
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
                var validBuildings = fittingLists[nextTypeIndex];

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
            var currentList = fittingLists[currentTypeIndex];
            var diffSize = currentList.Where(b => b.size != currentSize).ToList();
            if (diffSize.Count > 0) nextBuilding = diffSize[UnityEngine.Random.Range(0, diffSize.Count)];
        }
        else
        {
            var currentList = fittingLists[currentTypeIndex];
            var sameSize = currentList.Where(b => b.size == currentSize).ToList();
            if (sameSize.Count > 0) nextBuilding = sameSize[UnityEngine.Random.Range(0, sameSize.Count)];
        }

        if (nextBuilding == null)
        {
            nextTypeIndex = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
            var validBuildings = fittingLists[nextTypeIndex];
            nextBuilding = validBuildings[UnityEngine.Random.Range(0, validBuildings.Count)];
        }

        lastTypeIndex = nextTypeIndex;
        return nextBuilding;
    }

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