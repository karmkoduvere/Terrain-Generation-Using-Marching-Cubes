using DefaultNamespace;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MarchingCubesGPUOld : MonoBehaviour
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
        perlinDensity = WorldGenerator.Instance.NoiseScale /100f;
        noiseCutOff = 0.47f;
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

    List<Vector3> newVertices = new();
    List<int> newTriangles = new();
    List<Color> newColors = new();
    Dictionary<Vector3, int> verticeMap = new();

    ComputeBuffer testCountBuffer;
    ComputeBuffer testBuffer;
    ComputeBuffer testCounter;


    public void GenerateChunkSmooth(int LOD, GameObject chunk)
    {
        float time = Time.realtimeSinceStartup;

        int count = (int)Mathf.Pow(chunkSize/LOD, 3f)*(LOD+1);
        triangleBuffer = new ComputeBuffer(count, 72, ComputeBufferType.Append);// sizeof(float) * 4 * 6
        triangleBuffer.SetCounterValue(0);

        testBuffer = new ComputeBuffer(count*3, 4, ComputeBufferType.Default);// sizeof(float) * 4 * 6
        testBuffer.SetCounterValue(0);
        computeShader.SetBuffer(0, "test", testBuffer);

        testCounter = new ComputeBuffer(1, 4, ComputeBufferType.Raw);// sizeof(float) * 4 * 6
        testCounter.SetCounterValue(0);
        computeShader.SetBuffer(0, "counterBuffer", testCounter);

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


        triangleBuffer.GetData(vertexDataArray, 0, 0, counter[0]);

        int[] counter2 = new int[1];
        testCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        testCountBuffer.SetCounterValue(0);
        ComputeBuffer.CopyCount(testBuffer, testCountBuffer, 0);
        testCountBuffer.GetData(counter2);

        

        float[] testDataArray = new float[count * 3];


        testBuffer.GetData(testDataArray, 0, 0, count * 3);

        //print(counter2[0]);
        for (int i = 0; i < count * 3; i++)
        {
            //break;
            if (testDataArray[i] != 1 && testDataArray[i] != 2 && testDataArray[i] != 3)
            {
                print(i);
                print(count * 3 - i);
                break;
            }
        }

        num3 += (Time.realtimeSinceStartup - time);
        
        time = Time.realtimeSinceStartup;

        newVertices.Clear();
        newTriangles.Clear();
        newColors.Clear();
        verticeMap.Clear();

        int step = 0;
        for (int i = 0; i < counter[0]; i++)
        {
            Triangle triangle = vertexDataArray[i];
            if (triangle.Equals(default(Triangle))) break;

            ProcessVertex(triangle.vertex1, triangle.color1);
            ProcessVertex(triangle.vertex2, triangle.color2);
            ProcessVertex(triangle.vertex3, triangle.color3);
        }

        void ProcessVertex(Vector3 vertex, Vector3 color)
        {
            if (!verticeMap.TryGetValue(vertex, out int index))
            {
                newVertices.Add(vertex);
                newColors.Add(Color.HSVToRGB(color.x, color.y, color.z));
                verticeMap[vertex] = index = step++;
            }
            newTriangles.Add(index);
        }

     
        num4 += (Time.realtimeSinceStartup - time);
        
        time = Time.realtimeSinceStartup;

        Mesh mesh = new();

        mesh.SetVertices(newVertices);
        mesh.SetTriangles(newTriangles, 0, true);
        mesh.SetColors(newColors);
        mesh.RecalculateNormals();

        chunk.GetComponent<MeshFilter>().mesh = mesh;
        chunk.GetComponent<MeshRenderer>().material = material;

        num5+=(Time.realtimeSinceStartup - time);

        triangleBuffer.Release();
        triCountBuffer.Release();

        testBuffer.Release();
        testCountBuffer.Release();
        testCounter.Release();
    }
    
    float GetIndex(Vector3 vertex)
    {
        return (vertex.x + vertex.y * chunkSize + vertex.z * chunkSize * chunkSize);
    }

    private void OnDestroy()
    {
        triangleBuffer.Release();
        triCountBuffer.Release();

        testBuffer.Release();
        testCountBuffer.Release();
        testCounter.Release();
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
                        else if (noiseGenerator == NoiseGenerator.SimplePerlinTerrain)
                        {
                            noiseValue = PerlinNoise2D(x + xOffset, y + yOffset, z + zOffset);
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
