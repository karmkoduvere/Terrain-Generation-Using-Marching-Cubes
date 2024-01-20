using DefaultNamespace;
using System;
using UnityEngine;

public class MarchingCubesGPU : MonoBehaviour
{
    public GameObject ChunkLODPrefab;
    public ComputeShader computeShader;
    public ComputeShader noiseShader;

    private Vector3 worldSize;
    private int chunkSize;

    private readonly float[] LODTrainsitionDistances = new float[] { 
        0.4f, 0.2f, 0.1f, 0.05f 
    };

    private LODGroup group;

    private ComputeBuffer noisePointsBuffer;

    private ComputeBuffer triangelVerticesBuffer;
    private ComputeBuffer triangelsBuffer;
    private ComputeBuffer colorsBuffer;

    private ComputeBuffer verticeMapBuffer;
    private ComputeBuffer vertexCounterBuffer;
    private ComputeBuffer triangleCounterBuffer;

    private Vector3[] vertexDataArray;
    private int[] triangleDataArray;
    private Color[] colorDataArray;

    public void Generate(Vector3 offset)
    {
        worldSize = WorldGenerator.Instance.WorldSize;
        chunkSize = WorldGenerator.Instance.chunkSize;
        group = gameObject.GetComponent<LODGroup>();

        GenerateNoisePoints(offset);
        SetupComputeShader(offset);

        // Add 4 LOD levels
        LOD[] lods = new LOD[4];
        for (int i = 0; i < lods.Length; i++)
        {
            GameObject chunkLOD = Instantiate(
                ChunkLODPrefab,
                new Vector3(offset.x * chunkSize, offset.y * chunkSize, offset.z * chunkSize),
                Quaternion.identity,
                transform
            );
            chunkLOD.name = $"ChunkLOD_{i + 1}";
            GenerateChunk(i + 1, chunkLOD);
            lods[i] = new LOD(LODTrainsitionDistances[i], new Renderer[] { chunkLOD.GetComponent<MeshRenderer>() });
        }

        group.SetLODs(lods);
        group.RecalculateBounds();
        noisePointsBuffer.Release();
    }

    public void GenerateChunk(int LOD, GameObject chunk)
    {
        SetupBuffers(LOD);

        computeShader.SetInt("LOD", LOD);

        int threadGroupSize = chunkSize / LOD;
        if (threadGroupSize % 4 != 0) threadGroupSize = threadGroupSize / 4 + 1;
        else threadGroupSize /= 4;
        
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int z = 0; z < 2; z++)
                {
                    computeShader.SetVector("inChunkOffset", new Vector3(x,y,z));
                    computeShader.Dispatch(0, threadGroupSize, threadGroupSize, threadGroupSize);
                }
            }
        }

        RetrieveDataFromBuffers();

        Mesh mesh = new();

        mesh.SetVertices(vertexDataArray);
        mesh.SetTriangles(triangleDataArray, 0, true);
        mesh.SetColors(colorDataArray);
        mesh.RecalculateNormals();
        chunk.GetComponent<MeshFilter>().mesh = mesh;
        chunk.GetComponent<MeshRenderer>().material = WorldGenerator.Instance.Material;

        triangelVerticesBuffer.Release();
        triangelsBuffer.Release();
        colorsBuffer.Release();

        verticeMapBuffer.Release();
        vertexCounterBuffer.Release();
        triangleCounterBuffer.Release();
    }

    private void GenerateNoisePoints(Vector3 offset)
    {
        int size = (int)Mathf.Pow(chunkSize + 1, 3);
        noisePointsBuffer = new ComputeBuffer(size, sizeof(float), ComputeBufferType.Default);
        noisePointsBuffer.SetCounterValue(0);

        noiseShader.SetVector("offset", chunkSize * offset);
        noiseShader.SetInt("chunkSize", chunkSize);
        noiseShader.SetVector("worldSize", worldSize);
        noiseShader.SetBuffer(0, "noisePoints", noisePointsBuffer);

        noiseShader.SetFloat("noiseFrequency", WorldGenerator.Instance.NoiseScale / 100f);
        noiseShader.SetInt("octaves", WorldGenerator.Instance.Octaves);
        noiseShader.SetFloat("weightMultiplier", WorldGenerator.Instance.WeightMultiplier);
        noiseShader.SetFloat("noiseWeight", WorldGenerator.Instance.NoiseWeight);
        
        noiseShader.SetFloat("floorOffset", WorldGenerator.Instance.FloorOffset);
        noiseShader.SetFloat("hardFloor", WorldGenerator.Instance.HardFloor);
        noiseShader.SetFloat("hardFloorWeight", WorldGenerator.Instance.HardFloorWeight);

        noiseShader.SetFloat("offsetNoiseFrequency", WorldGenerator.Instance.OffsetNoiseScale / 100f);
        noiseShader.SetInt("offsetOctaves", WorldGenerator.Instance.OffsetOctaves);
        noiseShader.SetFloat("offsetWeight", WorldGenerator.Instance.OffsetWeight);
        
        noiseShader.SetInt("noiseGenerator", (int)WorldGenerator.Instance.NoiseGenerator);
        noiseShader.SetInt("offsetNoiseGenerator", (int)WorldGenerator.Instance.OffsetNoiseGenerator);
        
        noiseShader.SetVector("noiseOffset", WorldGenerator.Instance.NoiseOffset);

        noiseShader.Dispatch(0, chunkSize / 4 + 1, chunkSize / 4 + 1, chunkSize / 4 + 1);
    }

    private void SetupComputeShader(Vector3 offset)
    {
        computeShader.SetFloat("noiseCutOff", 0.0f);
        computeShader.SetBuffer(0, "noisePoints", noisePointsBuffer);
        computeShader.SetInt("chunkSize", chunkSize);
        computeShader.SetVector("offset", offset * chunkSize);
        computeShader.SetVector("worldSize", worldSize * chunkSize);
    }

    private void SetupBuffers(int LOD)
    {
        int size = chunkSize / LOD;
        int maxVerticeCount = 3 * (int)(MathF.Pow(size, 3) + 2 * MathF.Pow(size, 2) + size);
        int maxTrianglesCount = 12 * (int)MathF.Pow(size, 3);
        int verticeMapSize = (int)Mathf.Pow((size + 1) * 2, 3);

        triangelVerticesBuffer = new ComputeBuffer(maxVerticeCount, sizeof(float) * 3, ComputeBufferType.Default);
        triangelVerticesBuffer.SetCounterValue(0);

        colorsBuffer = new ComputeBuffer(maxVerticeCount, sizeof(float) * 4, ComputeBufferType.Default);
        colorsBuffer.SetCounterValue(0);

        triangelsBuffer = new ComputeBuffer(maxTrianglesCount, sizeof(uint), ComputeBufferType.Default);
        triangelsBuffer.SetCounterValue(0);

        verticeMapBuffer = new ComputeBuffer(verticeMapSize, sizeof(uint), ComputeBufferType.Default);
        verticeMapBuffer.SetCounterValue(0);

        vertexCounterBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);
        vertexCounterBuffer.SetCounterValue(0);

        triangleCounterBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);
        triangleCounterBuffer.SetCounterValue(0);

        computeShader.SetBuffer(0, "triangelVertices", triangelVerticesBuffer);
        computeShader.SetBuffer(0, "triangels", triangelsBuffer);
        computeShader.SetBuffer(0, "colors", colorsBuffer);
        computeShader.SetBuffer(0, "verticeMap", verticeMapBuffer);
        computeShader.SetBuffer(0, "vertexCounter", vertexCounterBuffer);
        computeShader.SetBuffer(0, "triangleCounter", triangleCounterBuffer);
    }

    private void RetrieveDataFromBuffers()
    {
        int[] vertexCounter = new int[1];
        vertexCounterBuffer.GetData(vertexCounter);

        int[] triangleCounter = new int[1];
        triangleCounterBuffer.GetData(triangleCounter);

        vertexDataArray = new Vector3[vertexCounter[0]];
        triangleDataArray = new int[triangleCounter[0]];
        colorDataArray = new Color[vertexCounter[0]];

        triangelVerticesBuffer.GetData(vertexDataArray, 0, 0, vertexCounter[0]);
        colorsBuffer.GetData(colorDataArray, 0, 0, vertexCounter[0]);
        triangelsBuffer.GetData(triangleDataArray, 0, 0, triangleCounter[0]);
    }


    private void OnDestroy()
    {
        
        triangelVerticesBuffer.Release();
        triangelsBuffer.Release();
        colorsBuffer.Release();

        verticeMapBuffer.Release();
        vertexCounterBuffer.Release();
        triangleCounterBuffer.Release();
        
        noisePointsBuffer.Release();
    }
}
