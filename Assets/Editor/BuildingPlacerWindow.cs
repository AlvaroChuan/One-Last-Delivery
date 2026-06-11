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

    // Space control variables
    private float _spacingFactor = 1.0f;
    private float _cornerGap = 6.0f;

    // Generation limits
    private int _maxBuildingsPerType = 0;
    private int _maxConsecutiveSameType = 3;

    // Save profile and UI references
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

    // ── TARGETED CLEAR METHOD ──
    void ClearGenerated()
    {
        if (_targetSpline == null)
        {
            Debug.LogError("Assign a City Block Spline first to target its specific container group.");
            return;
        }

        // Find the specific parent container for the selected spline to prevent wiping other generations
        string parentName = "Buildings_" + _targetSpline.name;
        GameObject parentGO = GameObject.Find(parentName);

        if (parentGO != null)
        {
            Undo.DestroyObjectImmediate(parentGO);
            Debug.Log($"Cleared generated buildings specifically for {_targetSpline.name}.");
        }
    }

    // ── CORE GENERATION ENGINE WITH TWO-PASS AND SCALED CAPPING ──
    void GenerateBuildings()
    {
        if (_targetSpline == null) return;

        List<List<BuildingData>> allResLists = new List<List<BuildingData>> { _profile.resA, _profile.resB, _profile.resC, _profile.resD };
        if (!allResLists.Any(list => list.Any(b => b != null && b.prefab != null))) return;

        // Clear only the current spline group before regenerating
        ClearGenerated();
        EnsureTag(_generatedTag);

        // Create a unique root parent container for this spline assignment
        string parentName = "Buildings_" + _targetSpline.name;
        GameObject parentGO = new GameObject(parentName);
        Undo.RegisterCreatedObjectUndo(parentGO, "Create Spline Buildings Root Container");
        Transform parentTransform = parentGO.transform;

        Spline spline = _targetSpline.Spline;
        int knotCount = spline.Count;

        int lastTypeIndex = -1;
        BuildingData lastBuilding = null;
        int[] typeCounters = new int[allResLists.Count];
        int consecutiveCount = 0;

        bool[] nodeHasCorner = new bool[knotCount];
        Vector3[] pCurrArray = new Vector3[knotCount];
        Vector3[] edgeDirArray = new Vector3[knotCount];
        Vector3[] outwardArray = new Vector3[knotCount];
        Quaternion[] rotArray = new Quaternion[knotCount];
        float[] cornerWidths = new float[knotCount];
        float[] cornerDepths = new float[knotCount];

        // ── PASS 1: INSTANTIATE FIXED CORNER ANCHORS ──
        for (int i = 0; i < knotCount; i++)
        {
            int iNext = (i + 1) % knotCount;
            int iPrev = (i - 1 + knotCount) % knotCount;

            Vector3 pCurr = _targetSpline.transform.TransformPoint(spline[i].Position);
            Vector3 pNext = _targetSpline.transform.TransformPoint(spline[iNext].Position);
            Vector3 pPrev = _targetSpline.transform.TransformPoint(spline[iPrev].Position);

            Vector3 edgeDir = (pNext - pCurr).normalized;
            Vector3 outward = new Vector3(-edgeDir.z, 0, edgeDir.x).normalized;

            Vector3 edgeMid = (pCurr + pNext) / 2f;
            Vector3 splineCenter = GetSplineCenter(spline, _targetSpline.transform);
            if (Vector3.Dot(outward, (splineCenter - edgeMid).normalized) > 0)
                outward = -outward;

            Quaternion rot = Quaternion.LookRotation(outward, Vector3.up);

            pCurrArray[i] = pCurr;
            edgeDirArray[i] = edgeDir;
            outwardArray[i] = outward;
            rotArray[i] = rot;

            Vector3 edgeIn = (pPrev - pCurr).normalized;
            bool isCorner = Vector3.Angle(edgeIn, edgeDir) > _cornerAngleThresh;

            if (isCorner && _profile.cornerPrefabs.Count > 0)
            {
                var validCorners = _profile.cornerPrefabs.Where(c => c != null).ToList();
                if (validCorners.Count > 0)
                {
                    nodeHasCorner[i] = true;
                    var prefab = validCorners[UnityEngine.Random.Range(0, validCorners.Count)];
                    Bounds b = GetPrefabBounds(prefab);

                    cornerWidths[i] = b.size.x;
                    cornerDepths[i] = b.size.z;

                    Vector3 pos = pCurr + edgeDir * (b.size.x / 2f) + outward * (-(_inset + b.size.z / 2f));
                    pos.y = 0;
                    PlacePrefab(prefab, pos, rot, parentTransform);
                }
            }
        }

        // ── PASS 2: GENERATE STRAIGHT CORE BUILDINGS ──
        for (int i = 0; i < knotCount; i++)
        {
            int iNext = (i + 1) % knotCount;

            float edgeLen = Vector3.Distance(pCurrArray[i], pCurrArray[iNext]);

            float currentCornerWidth = cornerWidths[i];
            float nextCornerDepth = nodeHasCorner[iNext] ? cornerDepths[iNext] : 0f;

            // Define straight safe zones boundaries based on gaps
            float coreStart = currentCornerWidth > 0f ? currentCornerWidth + _cornerGap : 0f;
            float coreEnd = edgeLen;
            if (nodeHasCorner[iNext]) coreEnd -= (nextCornerDepth + _cornerGap);

            if (coreStart > coreEnd)
            {
                coreStart = Mathf.Max(currentCornerWidth, edgeLen - nextCornerDepth);
                coreStart = Mathf.Min(coreStart, edgeLen);
                coreEnd = coreStart;
            }

            float cursor = coreStart;
            Vector3 vStart = pCurrArray[i];
            Vector3 edgeDir = edgeDirArray[i];
            Vector3 outward = outwardArray[i];
            Quaternion rot = rotArray[i];

            float firstPlacedCursor = -1f;
            float lastPlacedEndCursor = -1f;
            bool hasPlacedAnyStraight = false;

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

                if (!hasPlacedAnyStraight)
                {
                    firstPlacedCursor = cursor;
                    hasPlacedAnyStraight = true;
                }

                if (lastTypeIndex == prevTypeIndex) consecutiveCount++;
                else consecutiveCount = 1;

                lastBuilding = nextData;
                typeCounters[lastTypeIndex]++;

                Vector3 pos = vStart + edgeDir * (cursor + bw / 2f) + outward * (-(_inset + bd / 2f));
                pos.y = 0;

                PlacePrefab(nextData.prefab, pos, rot, parentTransform);
                cursor += bw;
                lastPlacedEndCursor = cursor;
            }

            float actualCoreEnd = cursor;

            // ── PASS 3: GAP SEALING WITH INDEPENDENT AXIS SCALING & MIN CAPPING ──

            // POST-CORNER FILLER (Start of the segment)
            if (nodeHasCorner[i] && coreStart > currentCornerWidth)
            {
                float gapStart = currentCornerWidth;
                float gapEnd = hasPlacedAnyStraight ? firstPlacedCursor : (edgeLen - nextCornerDepth);
                float gapSize = gapEnd - gapStart;

                if (gapSize > 0.1f)
                {
                    float origWidth;
                    BuildingData filler = FindBestBuildingForGap(allResLists, gapSize, out origWidth);
                    if (filler != null)
                    {
                        float xScale = gapSize / origWidth;
                        float yScale = Mathf.Max(xScale, 0.8f);
                        float zScale = Mathf.Max(xScale, 0.8f);

                        float posCursor = gapStart + gapSize / 2f;
                        float scaledDepth = GetPrefabBounds(filler.prefab).size.z * zScale;

                        Vector3 pos = vStart + edgeDir * posCursor + outward * (-(_inset + scaledDepth / 2f));
                        pos.y = 0;
                        PlaceScaledPrefab(filler.prefab, pos, rot, xScale, yScale, zScale, parentTransform);
                    }
                }
            }

            // PRE-CORNER FILLER (End of the segment)
            if (nodeHasCorner[iNext] && (edgeLen - nextCornerDepth) > actualCoreEnd)
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
                        float xScale = gapSize / origWidth;
                        float yScale = Mathf.Max(xScale, 0.8f);
                        float zScale = Mathf.Max(xScale, 0.8f);

                        float posCursor = gapStart + gapSize / 2f;
                        float scaledDepth = GetPrefabBounds(filler.prefab).size.z * zScale;

                        Vector3 pos = vStart + edgeDir * posCursor + outward * (-(_inset + scaledDepth / 2f));
                        pos.y = 0;
                        PlaceScaledPrefab(filler.prefab, pos, rot, xScale, yScale, zScale, parentTransform);
                    }
                }
            }
        }

        Debug.Log($"Generation Completed: Buildings successfully structured under hierarchy container node: {parentName}");
    }

    // ── SEARCHES FOR THE BEST BUILDING PROFILE FOR THE GAP SIZE ──
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

    // ── PLACES THE PREFAB APPLYING MODIFIED SCALE VECTOR UNDER PARENT ──
    void PlaceScaledPrefab(GameObject prefab, Vector3 pos, Quaternion rot, float xScale, float yScale, float zScale, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = pos;
        go.transform.rotation = rot * prefab.transform.rotation;
        go.transform.SetParent(parent);

        Vector3 newScale = go.transform.localScale;
        newScale.x *= xScale;
        newScale.y *= yScale;
        newScale.z *= zScale;
        go.transform.localScale = newScale;

        go.tag = _generatedTag;
        Undo.RegisterCreatedObjectUndo(go, "Place Scaled Building");
    }

    // ── PROCEDURAL PROBABILITY LOGIC FOR THE CENTRAL CORE ──
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

    void PlacePrefab(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = pos;
        go.transform.rotation = rot * prefab.transform.rotation;
        go.transform.SetParent(parent);
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