using UnityEngine;

public class QMCTerrain : MonoBehaviour
{
    [SerializeField] private ComputeShader _qmcComputeShader;

    [Header("Grass Settings")]
    [SerializeField] private GameObject _grassPrefab;
    [SerializeField] private int _maxPoints = 1000000; // ¡Dale caña, ahora la GPU puede con ello!
    [SerializeField] private Vector3 _grassSize = Vector3.one;

    [Header("Placement Rules")]
    [SerializeField] private float _maxSlopeAngle = 45f;

    // Terrain
    private Terrain _terrain;
    private RenderTexture _heightmapTexture;

    // Buffers de GPU
    private ComputeBuffer _matrixBuffer;
    private ComputeBuffer _argsBuffer;

    // Rendering
    private Material _instanceMaterial;
    private Mesh _grassMesh;
    private Bounds _bounds;

    void Start()
    {
        _terrain = GetComponent<Terrain>();
        TerrainData td = _terrain.terrainData;

        _grassMesh = _grassPrefab.GetComponent<MeshFilter>().sharedMesh;
        _instanceMaterial = _grassPrefab.GetComponent<MeshRenderer>().sharedMaterial;

        // Definimos un Bounds gigantesco para que Unity no haga culling de la malla original
        // (El culling real de la hierba lo harías por GPU en un sistema más avanzado)
        _bounds = new Bounds(_terrain.transform.position + td.size / 2f, td.size);

        // Heightmap RT
        _heightmapTexture = new RenderTexture(td.heightmapResolution, td.heightmapResolution, 0, RenderTextureFormat.RFloat);
        _heightmapTexture.enableRandomWrite = true;
        _heightmapTexture.Create();
        Graphics.Blit(td.heightmapTexture, _heightmapTexture);

        // 1. Buffer para las matrices (Stride = 64 bytes por cada float4x4)
        // Lo hacemos de tipo Append para que el Compute Shader meta solo la hierba válida.
        _matrixBuffer = new ComputeBuffer(_maxPoints, 64, ComputeBufferType.Append);
        _matrixBuffer.SetCounterValue(0); // Reseteamos el contador a 0

        // 2. Buffer de argumentos para DrawMeshInstancedIndirect
        _argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = (uint)_grassMesh.GetIndexCount(0);
        _argsBuffer.SetData(args);

        // Pasamos datos al Compute Shader
        int kernel = _qmcComputeShader.FindKernel("CSMain");
        _qmcComputeShader.SetTexture(kernel, "heightmap", _heightmapTexture);
        _qmcComputeShader.SetBuffer(kernel, "resultMatrices", _matrixBuffer);

        _qmcComputeShader.SetInt("pointBufferLength", _maxPoints);
        _qmcComputeShader.SetInt("terrainGridSizeX", td.heightmapResolution);
        _qmcComputeShader.SetInt("terrainGridSizeZ", td.heightmapResolution);
        _qmcComputeShader.SetVector("terrainSize", td.size);
        _qmcComputeShader.SetVector("terrainPosition", _terrain.transform.position);

        // Pasamos los parámetros de instanciación a la GPU
        _qmcComputeShader.SetFloat("maxSlopeCos", Mathf.Cos(_maxSlopeAngle * Mathf.Deg2Rad));
        _qmcComputeShader.SetVector("baseScale", _grassSize);

        // Despachamos el Compute Shader
        int threadGroups = Mathf.CeilToInt(_maxPoints / 256f);
        _qmcComputeShader.Dispatch(kernel, threadGroups, 1, 1);

        // Copiamos el número de elementos generados en el AppendBuffer al buffer de argumentos
        // El count va al índice 1 del argsBuffer (offset de 4 bytes)
        ComputeBuffer.CopyCount(_matrixBuffer, _argsBuffer, 4);

        // Le pasamos el buffer de matrices al material para que el shader de la hierba sepa dónde leerlas
        _instanceMaterial.SetBuffer("visibleInstances", _matrixBuffer);
    }

    void Update()
    {
        // Renderizado puro por GPU en una sola llamada, sin importar si son 10 mil o 2 millones.
        Graphics.DrawMeshInstancedIndirect(
            _grassMesh,
            0,
            _instanceMaterial,
            _bounds,
            _argsBuffer
        );
    }

    private void OnDestroy()
    {
        _matrixBuffer?.Release();
        _argsBuffer?.Release();
        _heightmapTexture?.Release();
    }
}