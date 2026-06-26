using UnityEngine;

public class QMCTerrain : MonoBehaviour
{
    [SerializeField] private ComputeShader _qmcComputeShader;

    [Header("Grass Settings")]
    [SerializeField] private GameObject _grassPrefab;
    [SerializeField] private int _maxPoints = 1000000;
    [SerializeField] private Vector3 _grassSize = Vector3.one;

    [Header("Placement Rules")]
    [SerializeField] private float _maxSlopeAngle = 45f;

    [Header("Optimization")]
    [SerializeField] private float _maxDrawDistance = 150f;

    private Camera _mainCamera;
    private Terrain _terrain;
    private RenderTexture _heightmapTexture;

    // Buffers de GPU
    private ComputeBuffer _allGrassBuffer; // Nuevo: Guarda datos precalculados
    private ComputeBuffer _matrixBuffer;   // El buffer de dibujado dinámico
    private ComputeBuffer _argsBuffer;

    private Material _instanceMaterial;
    private Mesh _grassMesh;
    private Bounds _bounds;

    private int _kernelGenerate;
    private int _kernelCull;
    private int _threadGroups;

    void Start()
    {
        _mainCamera = Camera.main;
        _terrain = GetComponent<Terrain>();
        TerrainData td = _terrain.terrainData;

        _grassMesh = _grassPrefab.GetComponent<MeshFilter>().sharedMesh;
        _instanceMaterial = _grassPrefab.GetComponent<MeshRenderer>().sharedMaterial;
        _bounds = new Bounds(_terrain.transform.position + td.size / 2f, td.size);

        _kernelGenerate = _qmcComputeShader.FindKernel("CSGenerate");
        _kernelCull = _qmcComputeShader.FindKernel("CSCull");
        _threadGroups = Mathf.CeilToInt(_maxPoints / 256f);

        // Heightmap RT
        _heightmapTexture = new RenderTexture(td.heightmapResolution, td.heightmapResolution, 0, RenderTextureFormat.RFloat);
        _heightmapTexture.enableRandomWrite = true;
        _heightmapTexture.Create();
        Graphics.Blit(td.heightmapTexture, _heightmapTexture);

        // --- INICIALIZAR BUFFERS ---
        // Stride 36 = (float3 pos = 12) + (float3 normal = 12) + (float3 scale = 12)
        _allGrassBuffer = new ComputeBuffer(_maxPoints, 36);

        _matrixBuffer = new ComputeBuffer(_maxPoints, 64, ComputeBufferType.Append);

        _argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = (uint)_grassMesh.GetIndexCount(0);
        _argsBuffer.SetData(args);

        // --- ASIGNAR VARIABLES ESTATICAS ---
        _qmcComputeShader.SetInt("pointBufferLength", _maxPoints);
        _qmcComputeShader.SetInt("terrainGridSizeX", td.heightmapResolution);
        _qmcComputeShader.SetInt("terrainGridSizeZ", td.heightmapResolution);
        _qmcComputeShader.SetVector("terrainSize", td.size);
        _qmcComputeShader.SetVector("terrainPosition", _terrain.transform.position);
        _qmcComputeShader.SetFloat("maxSlopeCos", Mathf.Cos(_maxSlopeAngle * Mathf.Deg2Rad));
        _qmcComputeShader.SetVector("baseScale", _grassSize);

        // --- PASO 1: EJECUTAR GENERACION UNA SOLA VEZ ---
        _qmcComputeShader.SetTexture(_kernelGenerate, "heightmap", _heightmapTexture);
        _qmcComputeShader.SetBuffer(_kernelGenerate, "allGrassBuffer", _allGrassBuffer);
        _qmcComputeShader.Dispatch(_kernelGenerate, _threadGroups, 1, 1);

        // Ya no necesitamos la textura del heightmap, podemos liberarla de la memoria
        _heightmapTexture.Release();

        // Asignamos los buffers para el paso de Culling que ocurrirá en el Update
        _qmcComputeShader.SetBuffer(_kernelCull, "allGrassBuffer", _allGrassBuffer);
        _qmcComputeShader.SetBuffer(_kernelCull, "resultMatrices", _matrixBuffer);

        _instanceMaterial.SetBuffer("visibleInstances", _matrixBuffer);
    }

    void Update()
    {
        if (_mainCamera == null) return;

        // 1. Vaciar el buffer de dibujado
        _matrixBuffer.SetCounterValue(0);

        // 2. Actualizar variables dinámicas
        _qmcComputeShader.SetVector("cameraPosition", _mainCamera.transform.position);
        _qmcComputeShader.SetVector("cameraForward", _mainCamera.transform.forward);
        _qmcComputeShader.SetFloat("maxDistance", _maxDrawDistance);

        // --- PASO 2: EJECUTAR CULLING (Ultra rapido) ---
        _qmcComputeShader.Dispatch(_kernelCull, _threadGroups, 1, 1);

        // Copiar conteo y dibujar sin sombras
        ComputeBuffer.CopyCount(_matrixBuffer, _argsBuffer, 4);

        Graphics.DrawMeshInstancedIndirect(
            _grassMesh,
            0,
            _instanceMaterial,
            _bounds,
            _argsBuffer,
            0,
            null,
            UnityEngine.Rendering.ShadowCastingMode.Off, // Sin sombras proyectadas
            false,                                       // Sin recibir sombras
            gameObject.layer
        );
    }

    private void OnDestroy()
    {
        _allGrassBuffer?.Release();
        _matrixBuffer?.Release();
        _argsBuffer?.Release();
        if (_heightmapTexture != null) _heightmapTexture.Release();
    }
}