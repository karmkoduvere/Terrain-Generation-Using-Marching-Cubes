using DefaultNamespace;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

public class MarchingCubesGPUExperimental : MonoBehaviour
{
    public GameObject ChunkLODPrefab;
    public GameObject Marker;
    public bool Mark;

    private Vector3 wordlSize;
    private int chunkSize;
    private float perlinDensity;
    private float noiseCutOff;

    private NoiseGenerator noiseGenerator;
    private Material material;

    private float[] LODTrainsitionDistances = new float[] { 0.4f, 0.2f, 0.1f, 0.05f }; // Array of distances for LOD transitions
    private LODGroup group;

    public static float num1 = 0;
    public static float num2 = 0;
    public static float num3 = 0;
    public static float num4 = 0;
    public static float num5 = 0;


    public ComputeShader computeShader;
    public Texture3D noiseTexture;
 
    public void Generate(Vector3 offset)
    {
        wordlSize = WorldGenerator.Instance.WorldSize;
        chunkSize = WorldGenerator.Instance.chunkSize;
        perlinDensity = WorldGenerator.Instance.NoiseScale /100f;
        noiseCutOff = WorldGenerator.Instance.NoiseCutOff;
        noiseGenerator = WorldGenerator.Instance.NoiseGenerator;
        material = WorldGenerator.Instance.Material;
        group = gameObject.GetComponent<LODGroup>();

        float time = Time.realtimeSinceStartup;
        CreateNoiseTexture(offset);
        num1 += Time.realtimeSinceStartup - time;

        computeShader.SetTexture(0, "NoiseTextrure", noiseTexture);
        computeShader.SetFloat("noiseCutOff", noiseCutOff);
        computeShader.SetInt("chunkSize", chunkSize);
        computeShader.SetVector("offset", offset * chunkSize);
        computeShader.SetVector("wordlSize", wordlSize * chunkSize);

        // Add 4 LOD levels
        LOD[] lods = new LOD[1];
        for (int i = 0; i < lods.Length; i++)
        {

            GameObject chunkLOD = Instantiate(
                ChunkLODPrefab,
                new Vector3(offset.x * chunkSize, offset.y * chunkSize, offset.z * chunkSize),
                Quaternion.identity,
                transform
            );
            chunkLOD.name = $"ChunkLOD_{i + 1}";
            GenerateChunkSmooth(i + 1, chunkLOD);
            lods[i] = new LOD(LODTrainsitionDistances[i], new Renderer[] { chunkLOD.GetComponent<MeshRenderer>() });
        }

        group.SetLODs(lods);
        group.RecalculateBounds();
    }



    ComputeBuffer triangelVerticesBuffer;
    ComputeBuffer triangelsBuffer;
    ComputeBuffer colorsBuffer;

    ComputeBuffer verticeMapBuffer;
    ComputeBuffer vertexCounterBuffer;
    ComputeBuffer triangleCounterBuffer;

    /*
     
    RWTexture3D<uint> verticeMap;

    RWStructuredBuffer<float3> triangelVertices;
    RWStructuredBuffer<uint> triangels;
    RWStructuredBuffer<float3> colors;

    RWStructuredBuffer<uint> vertexCounter;
    RWStructuredBuffer<uint> triangleCounter;
     */


    public void GenerateChunkSmooth(int LOD, GameObject chunk)
    {
        float time = Time.realtimeSinceStartup;

        int size = chunkSize / LOD;
        int maxVerticeCount = 3 * (int)(MathF.Pow(size, 3) + 2 * MathF.Pow(size, 2) + size);
        int maxTrianglesCount = 4 * (int)MathF.Pow(size, 3);
        int verticeMapSize = (int)Mathf.Pow((size + 1) * 2, 3);

        triangelVerticesBuffer = new ComputeBuffer(maxVerticeCount, sizeof(float) * 3, ComputeBufferType.Default);
        triangelVerticesBuffer.SetCounterValue(0);

        colorsBuffer = new ComputeBuffer(maxVerticeCount, sizeof(float) * 3, ComputeBufferType.Default);
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

        computeShader.SetInt("LOD", LOD);

        //computeShader.Dispatch(0, chunkSize/LOD, chunkSize/LOD, chunkSize/LOD);

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int z = 0; z < 2; z++)
                {
                    computeShader.SetVector("inChunkOffset", new Vector3(x,y,z));
                    computeShader.Dispatch(0, chunkSize / LOD / 4, chunkSize / LOD / 4, chunkSize / LOD / 4);
                }
            }
        }
        
        num2 += Time.realtimeSinceStartup - time;
        time = Time.realtimeSinceStartup;

        int[] vertexCounter = new int[1];
        vertexCounterBuffer.GetData(vertexCounter);

        int[] triangleCounter = new int[1];
        triangleCounterBuffer.GetData(triangleCounter);

        /*
        Vector3[] vertexDataArray = new Vector3[vertexCounter[0]];
        int[] triangleDataArray = new int[triangleCounter[0]];
        //Vector3[] colorDataArray = new Vector3[vertexCounter[0]]

        triangelVerticesBuffer.GetData(vertexDataArray, 0, 0, vertexCounter[0]);
        triangelsBuffer.GetData(triangleDataArray, 0, 0, triangleCounter[0]);
        */

        num3 += (Time.realtimeSinceStartup - time);
        time = Time.realtimeSinceStartup;

        Mesh mesh = new();

        TransferVertexData(mesh, vertexCounter[0]);
        TransferIndexData(mesh, triangleCounter[0]);
        

        //mesh.SetVertices(vertexDataArray);
        //mesh.SetTriangles(triangleDataArray, 0, true);
        //mesh.SetColors(newColors);

        num4 += (Time.realtimeSinceStartup - time);
        time = Time.realtimeSinceStartup;

        mesh.RecalculateNormals();
        chunk.GetComponent<MeshFilter>().mesh = mesh;
        chunk.GetComponent<MeshRenderer>().material = material;

        num5+=(Time.realtimeSinceStartup - time);

        triangelVerticesBuffer.Release();
        triangelsBuffer.Release();
        colorsBuffer.Release();

        verticeMapBuffer.Release();
        vertexCounterBuffer.Release();
        triangleCounterBuffer.Release();
    }

    private unsafe void TransferVertexData(Mesh mesh, int vertexCount)
    {
        mesh.SetVertexBufferData(
            NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3>(
                triangelVerticesBuffer.GetNativeBufferPtr().ToPointer(),
                vertexCount,
                Allocator.Invalid),
            0,
            0,
            vertexCount);
    }

    private unsafe void TransferIndexData(Mesh mesh, int indexCount)
    {
        mesh.SetIndexBufferData(
            NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3>(
                triangelsBuffer.GetNativeBufferPtr().ToPointer(),
                indexCount,
                Allocator.Invalid),
            0,
            0,
            indexCount);
    }

    private void OnDestroy()
    {
        triangelVerticesBuffer.Release();
        triangelsBuffer.Release();
        colorsBuffer.Release();

        verticeMapBuffer.Release();
        vertexCounterBuffer.Release();
        triangleCounterBuffer.Release();
    }

    // Generates noise points with 3D array the size of lenght * height * depth
    void CreateNoiseTexture(Vector3 offset)
    {
        int xOffset = chunkSize * (int)offset.x;
        int yOffset = chunkSize * (int)offset.y;
        int zOffset = chunkSize * (int)offset.z;

        noiseTexture = new Texture3D(chunkSize+1, chunkSize+1, chunkSize+1, TextureFormat.ARGB32, false);
        UnityEngine.Color[] colors = new UnityEngine.Color[(int)Mathf.Pow(chunkSize+1,3)];

        for (int z = 0; z < chunkSize + 1; z++)
        {
            for (int y = 0; y < chunkSize + 1; y++)
            {
                for (int x = 0; x < chunkSize + 1; x++)
                {
                    float noiseValue = 0;
                    if (z + zOffset == 0 || y + yOffset == 0 || x + xOffset == 0 || z + zOffset == wordlSize.z * chunkSize || y + yOffset == wordlSize.y * chunkSize || x + xOffset == wordlSize.x * chunkSize)
                        noiseValue = 0;
                    else 
                    {
                        if (noiseGenerator == NoiseGenerator.PerlinNoise3D)
                        {
                            noiseValue = PerlinNoise3D(x + xOffset, y + yOffset, z + zOffset);
                        }
                        else if (noiseGenerator == NoiseGenerator.PerlinNoise2D)
                        {
                            noiseValue = PerlinNoise2D(x + xOffset, y + yOffset, z + zOffset);
                        }
                        else if (noiseGenerator == NoiseGenerator.Perlid2DX3D)
                        {
                            noiseValue = PerlinNoise2DX3D(x + xOffset, y + yOffset, z + zOffset);
                        }
                    }
                    colors[x + y * (chunkSize + 1) + z * (chunkSize + 1)* (chunkSize + 1)] = new UnityEngine.Color(noiseValue, 0, 0, 1);
                }
            }
        }
        noiseTexture.SetPixels(colors);
        noiseTexture.Apply();
    }

    #region Noises
    private float PerlinNoise2DX3D(float x, float y, float z)
    {
        return (PerlinNoise2D(x, y, z) == 0) ? 0 : PerlinNoise3D(x, y, z);
    }

    private float PerlinNoise2D(float x, float y, float z)
    {
        int height = chunkSize * (int)wordlSize.y + 1;
        x *= perlinDensity;
        z *= perlinDensity;
        float xzPerlinHeight = Mathf.PerlinNoise(x, z);

        if (height * xzPerlinHeight > y)
        {
            if (height * xzPerlinHeight - y < 1.0) return height * xzPerlinHeight - y;
            return 1;
        }
        return 0;
    }

    private float PerlinNoise3D(float x, float y, float z)
    {
        x *= perlinDensity;
        y *= perlinDensity;
        z *= perlinDensity;
        float XY = Mathf.PerlinNoise(x, y);
        float XZ = Mathf.PerlinNoise(x, z);
        float YX = Mathf.PerlinNoise(y, x);
        float YZ = Mathf.PerlinNoise(y, z);
        float ZX = Mathf.PerlinNoise(z, x);
        float ZY = Mathf.PerlinNoise(z, y);

        float XYZ = XY + XZ + YX + YZ + ZX + ZY;

        return XYZ / 6f;
    }
    #endregion

    
}
