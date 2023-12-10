using DefaultNamespace;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class MarchingCubesGPU : MonoBehaviour
{
    public ComputeShader computeShader;
    ComputeBuffer triangleBuffer;

    struct Triangle
    {
        public Vector3 vertex1;
        public Vector3 vertex2;
        public Vector3 vertex3;
        
        public Vector3 color1;
        public Vector3 color2;
        public Vector3 color3;
    };

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

    //public RenderTexture noiseTexture;
    public Texture3D noiseTexture;
    public ComputeBuffer triCountBuffer;
    Triangle[] vertexDataArray;

    public static float num1 = 0;
    public static float num2 = 0;
    public static float num3 = 0;
    public static float num4 = 0;
    public static float num5 = 0;

    public void Generate(Vector3 offset)
    {
        wordlSize = WorldGenerator.Instance.WorldSize;
        chunkSize = WorldGenerator.Instance.chunkSize;
        perlinDensity = WorldGenerator.Instance.PerlinDensity;
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

    public void GenerateChunkSmooth(int LOD, GameObject chunk)
    {
        float time = Time.realtimeSinceStartup;

        int count = (int)Mathf.Pow(chunkSize/LOD, 3f)*(LOD+1);
        triangleBuffer = new ComputeBuffer(count, 72, ComputeBufferType.Append);// sizeof(float) * 4 * 6
        triangleBuffer.SetCounterValue(0);

        computeShader.SetInt("LOD", LOD);
        computeShader.SetBuffer(0, "triangles", triangleBuffer);
        //computeShader.Dispatch(0, chunkSize/LOD, chunkSize/LOD, chunkSize/LOD);
        computeShader.Dispatch(0, chunkSize / LOD / 4, chunkSize / LOD / 4, chunkSize / LOD / 4);
        
        num2 += Time.realtimeSinceStartup - time;
        time = Time.realtimeSinceStartup;


        int[] counter = new int[1];
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        triCountBuffer.SetCounterValue(0);
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        triCountBuffer.GetData(counter);
        
        vertexDataArray = new Triangle[counter[0]];


        //Debug.Log("Number of elements in the buffer: " + counter[0] +" "+ count);

        //print("lod_"+LOD + ": " + (float)count /counter[0]);

        triangleBuffer.GetData(vertexDataArray, 0, 0, counter[0]);

        num3 += (Time.realtimeSinceStartup - time);
        
        time = Time.realtimeSinceStartup;
        
        //List<Vector3> newVertices = new();
        //List<int> newTriangles = new();
        //List<UnityEngine.Color> newColors = new();

        Vector3[] newVertices = new Vector3[counter[0] * 3];
        int[] newTriangles = new int[counter[0] * 3];
        UnityEngine.Color[] newColors = new UnityEngine.Color[counter[0] * 3];

        Dictionary<Vector3, int> verticeMap = new();

        int step = 0;
        int triStep = 0;

        for (int i = 0; i < counter[0]; i++)
        {
            Triangle triangle = vertexDataArray[i];
            if (triangle.Equals(default(Triangle))) break;

            if (!verticeMap.ContainsKey(triangle.vertex1))
            {
                newVertices[step] = triangle.vertex1;
                newColors[step] = UnityEngine.Color.HSVToRGB(triangle.color1.x, triangle.color1.y, triangle.color1.z);
                verticeMap[triangle.vertex1] = step++;
            }
            newTriangles[triStep++] = verticeMap[triangle.vertex1];

            if (!verticeMap.ContainsKey(triangle.vertex2))
            {
                newVertices[step] = triangle.vertex2;
                newColors[step] = UnityEngine.Color.HSVToRGB(triangle.color2.x, triangle.color2.y, triangle.color2.z);
                verticeMap[triangle.vertex2] = step++;
            }
            newTriangles[triStep++] = verticeMap[triangle.vertex2];

            if (!verticeMap.ContainsKey(triangle.vertex3))
            {
                newVertices[step] = triangle.vertex3;
                newColors[step] = UnityEngine.Color.HSVToRGB(triangle.color3.x, triangle.color3.y, triangle.color3.z);
                verticeMap[triangle.vertex3] = step++;
            }
            newTriangles[triStep++] = verticeMap[triangle.vertex3];
        }
        /*
         int step = 0;

        for (int i = 0; i < counter[0]; i++)
        {
            Triangle triangle = vertexDataArray[i];
            if (triangle.Equals(default(Triangle))) break;

            if (!verticeMap.ContainsKey(triangle.vertex1))
            {
                newVertices.Add(triangle.vertex1);
                newColors.Add(UnityEngine.Color.HSVToRGB(triangle.color1.x, triangle.color1.y, triangle.color1.z));
                verticeMap[triangle.vertex1] = step++;
            }
            newTriangles.Add(verticeMap[triangle.vertex1]);

            if (!verticeMap.ContainsKey(triangle.vertex2))
            {
                newVertices.Add(triangle.vertex2);
                newColors.Add(UnityEngine.Color.HSVToRGB(triangle.color2.x, triangle.color2.y, triangle.color2.z));
                verticeMap[triangle.vertex2] = step++;
            }
            newTriangles.Add(verticeMap[triangle.vertex2]);

            if (!verticeMap.ContainsKey(triangle.vertex3))
            {
                newVertices.Add(triangle.vertex3);
                newColors.Add(UnityEngine.Color.HSVToRGB(triangle.color3.x, triangle.color3.y, triangle.color3.z));
                verticeMap[triangle.vertex3] = step++;
            }
            newTriangles.Add(verticeMap[triangle.vertex3]);
        }
         */

        /*
        Dictionary<float, int> verticeMap = new();

        int step = 0;

        for (int i = 0; i < counter[0]; i++)
        {
            Triangle triangle = vertexDataArray[i];
            if (triangle.Equals(default(Triangle))) break;

            if (!verticeMap.ContainsKey(GetIndex(triangle.vertex1)))
            {
                newVertices.Add(triangle.vertex1);
                newColors.Add(UnityEngine.Color.HSVToRGB(triangle.color1.x, triangle.color1.y, triangle.color1.z));
                verticeMap[GetIndex(triangle.vertex1)] = step++;
            }
            newTriangles.Add(verticeMap[GetIndex(triangle.vertex1)]);

            if (!verticeMap.ContainsKey(GetIndex(triangle.vertex2)))
            {
                newVertices.Add(triangle.vertex2);
                newColors.Add(UnityEngine.Color.HSVToRGB(triangle.color2.x, triangle.color2.y, triangle.color2.z));
                verticeMap[GetIndex(triangle.vertex2)] = step++;
            }
            newTriangles.Add(verticeMap[GetIndex(triangle.vertex2)]);

            if (!verticeMap.ContainsKey(GetIndex(triangle.vertex3)))
            {
                newVertices.Add(triangle.vertex3);
                newColors.Add(UnityEngine.Color.HSVToRGB(triangle.color3.x, triangle.color3.y, triangle.color3.z));
                verticeMap[GetIndex(triangle.vertex3)] = step++;
            }
            newTriangles.Add(verticeMap[GetIndex(triangle.vertex3)]);
        }*/

        num4 += (Time.realtimeSinceStartup - time);
        
        time = Time.realtimeSinceStartup;

        /*
        Mesh mesh = new()
        {
            vertices = newVertices.ToArray(),
            triangles = newTriangles.ToArray(),
            colors = newColors.ToArray()
        };*/

        Mesh mesh = new()
        {
            vertices = newVertices,
            triangles = newTriangles,
            colors = newColors
        };

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        chunk.GetComponent<MeshFilter>().mesh = mesh;
        chunk.GetComponent<MeshRenderer>().material = material;

        num5+=(Time.realtimeSinceStartup - time);

        triangleBuffer.Release();
        triCountBuffer.Release();
    }
    
    float GetIndex(Vector3 vertex)
    {
        return (vertex.x + vertex.y * chunkSize + vertex.z * chunkSize * chunkSize);
    }

    private void OnDestroy()
    {
        triangleBuffer.Release();
        triCountBuffer.Release();
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
