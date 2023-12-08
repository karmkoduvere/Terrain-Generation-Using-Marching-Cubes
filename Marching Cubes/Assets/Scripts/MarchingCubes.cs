using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class MarchingCubes : MonoBehaviour
{    
    private Vector3 wordlSize;
    private int chunkSize;
    private float perlinDensity;
    private float noiseCutOff;
    
    private NoiseGenerator noiseGenerator;
    private Material material;

    private float[][][] noisePoints;

    public GameObject ChunkPrefab;

    public GameObject Marker;
    public bool Mark;

    private float[] LODTrainsitionDistances = new float[] { 0.4f, 0.2f, 0.1f, 0.05f }; // Array of distances for LOD transitions
    private LODGroup group;

    private void Start()
    {
        
    }

    [ContextMenu("test")]
    void HighlightVertices()
    {
        var sum = 0.0;
        foreach (LOD lod in group.GetLODs())
        {
            sum += lod.screenRelativeTransitionHeight;
            print(lod.screenRelativeTransitionHeight);

        }
        print(sum);
    }

    public void Generate(Vector3 offset)
    {
        wordlSize = WorldGenerator.Instance.WorldSize;
        chunkSize = WorldGenerator.Instance.chunkSize;
        perlinDensity = WorldGenerator.Instance.PerlinDensity;
        noiseCutOff = WorldGenerator.Instance.NoiseCutOff;
        noiseGenerator = WorldGenerator.Instance.NoiseGenerator;
        material = WorldGenerator.Instance.Material;
        group = gameObject.GetComponent<LODGroup>();

        noisePoints = CreatePoints(offset);

        // Add 4 LOD levels
        LOD[] lods = new LOD[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject chunkLOD = Instantiate(
                ChunkPrefab,
                new Vector3(offset.x * chunkSize, offset.y * chunkSize, offset.z * chunkSize),
                Quaternion.identity,
                transform
            );
            GenerateChunkSmooth(offset, (i+1),chunkLOD);
            print(LODTrainsitionDistances[i]);
            lods[i] = new LOD(LODTrainsitionDistances[i], new Renderer[] { chunkLOD.GetComponent<MeshRenderer>() });            
        }

        group.SetLODs(lods);
        group.RecalculateBounds();
    }

    public void GenerateChunkSmooth(Vector3 offset, int LOD, GameObject chunk)
    {
        List<Vector3> newVertices = new();
        List<int> newTriangles = new();
        List<Color> colors = new();

        Dictionary<Vector3, int> verticeMap = new();

        // "Marches" over the chunk, at every point gets TriangeTable index based on
        // the generated points and then adds new vertices and triangles to the list.
        int step = 0;
        for (int z = 0; z < chunkSize; z += LOD)
        {
            for (int y = 0; y < chunkSize; y += LOD)
            {
                for (int x = 0; x < chunkSize; x += LOD)
                {
                    int index = GetTriangleIndex(x, y, z,LOD);
                    foreach (int el in TriangleTable[index])
                    {
                        if (el == -1) break;
                        Vector3 vertex = new Vector3(x, y, z) + cubeEdgeOffset[el] * LOD;

                        if (!verticeMap.ContainsKey(vertex))
                        {
                            if (Mark) Instantiate(Marker, new Vector3(x, y, z) + CalcVertexPos(x, y, z, el, LOD) * LOD, Quaternion.identity, chunk.transform);

                            newVertices.Add(new Vector3(x, y, z) + CalcVertexPos(x, y, z, el,LOD)*LOD);// cubeEdgeOffset[el]*LOD
                            verticeMap[vertex] = step++;
                            float colorValueX = (vertex.x + chunkSize * offset.x) / (wordlSize.x * chunkSize);
                            float colorValueY = (vertex.y + chunkSize * offset.y) / (wordlSize.y * chunkSize);
                            float colorValueZ = (vertex.z + chunkSize * offset.z) / (wordlSize.z * chunkSize);
                            colors.Add(Color.HSVToRGB(colorValueX / 2 + colorValueZ / 2, colorValueY * 1 / 3 + 0.25f, colorValueY));
                        }

                        newTriangles.Add(verticeMap[vertex]);
                    }
                }
            }
        }

        Mesh mesh = new()
        {
            vertices = newVertices.ToArray(),
            triangles = newTriangles.ToArray(),
            colors = colors.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        chunk.GetComponent<MeshFilter>().mesh = mesh;
        chunk.GetComponent<MeshRenderer>().material = material;
    }

    Vector3 CalcVertexPos(int x, int y, int z, int n, int LOD)
    {
        Vector3Int pointA = new Vector3Int(x, y, z) + vertices[edgeVertexIndices[n][0]]*LOD;
        Vector3Int pointB = new Vector3Int(x, y, z) + vertices[edgeVertexIndices[n][1]] * LOD;

        float pos = (noiseCutOff - noisePoints[pointA.z][pointA.y][pointA.x]) /
            (noisePoints[pointB.z][pointB.y][pointB.x] - noisePoints[pointA.z][pointA.y][pointA.x]);

        return n switch
        {
            0 or 2 or 4 or 6 => vertices[edgeVertexIndices[n][0]] + new Vector3(pos, 0, 0),
            1 or 3 or 5 or 7 => vertices[edgeVertexIndices[n][0]] + new Vector3(0, pos, 0),
            8 or 9 or 10 or 11 => vertices[edgeVertexIndices[n][0]] + new Vector3(0, 0, pos),
            _ => new Vector3(0, 0, 0)
        };
    }

    public void GenerateChunk(Vector3 offset, GameObject chunk)
    {
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();
        List<Color> colors = new();

        // "Marches" over the chunk, at every point gets TriangeTable index based on
        // the generated points and then adds new vertices and triangles to the list.
        int step = 0;        
        for (int z = 0; z < chunkSize; z++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    int index = GetTriangleIndex(x, y, z,1);
                    foreach (int el in TriangleTable[index])
                    {
                        if (el == -1) break;
                        newVertices.Add(new Vector3(x,y,z) + cubeEdgeOffset[el]);
                        newTriangles.Add(step);
                        // Map x,y,z positions/offsets to 0-1
                        float colorValueX = (float)((x * 0.1 + offset.x) / (chunkSize * 0.1 + chunkSize));
                        float colorValueY = (float)((y * 0.1 + offset.y) / (chunkSize * 0.1 + chunkSize));
                        float colorValueZ = (float)((z * 0.1 + offset.z) / (chunkSize * 0.1 + chunkSize));

                        colors.Add(Color.HSVToRGB(colorValueX/2 + colorValueZ/2, colorValueY * 1/3 + 0.25f, colorValueY));
                        step++;
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = newVertices.ToArray();
        mesh.triangles = newTriangles.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();
        chunk.GetComponent<MeshFilter>().mesh = mesh;
        //chunk.AddComponent<MeshCollider>().sharedMesh = mesh; // Collision
        chunk.GetComponent<MeshRenderer>().material = material;
        mesh.RecalculateBounds();
        //mesh.Optimize();
    }

    // Calculates index for the TriangleTable based on generated noisePoints and given cords
    int GetTriangleIndex(int x, int y, int z, int LOD)
    {
        int offset = LOD;
        string bi = ((noisePoints[z + offset][y + offset][x + offset] < noiseCutOff) ? "0" : "1") +
                    ((noisePoints[z + offset][y + offset][x] < noiseCutOff) ? "0" : "1") +
                    ((noisePoints[z + offset][y][x + offset] < noiseCutOff) ? "0" : "1") +
                    ((noisePoints[z + offset][y][x] < noiseCutOff) ? "0" : "1") +
                    ((noisePoints[z][y + offset][x + offset] < noiseCutOff) ? "0" : "1") +
                    ((noisePoints[z][y + offset][x] < noiseCutOff) ? "0" : "1") +
                    ((noisePoints[z][y][x + offset] < noiseCutOff) ? "0" : "1") +
                    ((noisePoints[z][y][x] < noiseCutOff) ? "0" : "1");

        return Convert.ToInt32(bi, 2);
    }

    // Generates noise points with 3D array the size of lenght * height * depth
    float[][][] CreatePoints(Vector3 offset)
    {
        int xOffset = chunkSize * (int)offset.x;
        int yOffset = chunkSize * (int)offset.y;
        int zOffset = chunkSize * (int)offset.z;

        float[][][] points = new float[chunkSize+1][][];
        for (int z = 0; z < chunkSize+1; z++)
        {
            points[z] = new float[chunkSize+1][];
            for (int y = 0; y < chunkSize+1; y++)
            {
                points[z][y] = new float[chunkSize+1];
                for (int x = 0; x < chunkSize+1; x++)
                {
                    //if (Mark) Instantiate(Marker, new Vector3(x, y, z), Quaternion.identity, transform);
                    if (z+zOffset == 0 || y + yOffset == 0 || x + xOffset == 0 || z+zOffset == wordlSize.z*chunkSize || y + yOffset == wordlSize.y * chunkSize || x + xOffset == wordlSize.x * chunkSize)
                        points[z][y][x] = 0;
                    else
                    {
                        if (noiseGenerator == NoiseGenerator.PerlinNoise3D)
                        {
                            points[z][y][x] = PerlinNoise3D(x + xOffset, y + yOffset, z + zOffset);
                        }
                        else if (noiseGenerator == NoiseGenerator.PerlinNoise2D)
                        {
                            points[z][y][x] = PerlinNoise2D(x + xOffset, y + yOffset, z + zOffset);
                        }
                        else if (noiseGenerator == NoiseGenerator.Perlid2DX3D)
                        {
                            points[z][y][x] = PerlinNoise2DX3D(x + xOffset, y + yOffset, z + zOffset);
                        }
                    }
                }
            }
        }
        return points;
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

    #region Arrays used for marching cubes

    private static readonly Vector3[] cubeEdgeOffset = {
        new Vector3(0.5f,0,0),
        new Vector3(1,0.5f,0),
        new Vector3(0.5f,1,0),
        new Vector3(0,0.5f,0),
        new Vector3(0.5f,0,1),
        new Vector3(1,0.5f,1),
        new Vector3(0.5f,1,1),
        new Vector3(0,0.5f,1),
        new Vector3(0,0,0.5f),
        new Vector3(1,0,0.5f),
        new Vector3(1,1,0.5f),
        new Vector3(0,1,0.5f)
    };

    private static readonly int[][] edgeVertexIndices = new int[][]{
        new int[]{0, 1},
        new int[]{1, 3},
        new int[]{2, 3},
        new int[]{0, 2},
        new int[]{4, 5},
        new int[]{5, 7},
        new int[]{6, 7},
        new int[]{4, 6},
        new int[]{0, 4},
        new int[]{1, 5},
        new int[]{3, 7},
        new int[]{2, 6}
    };

    private static readonly Vector3Int[] vertices = new Vector3Int[]
    {
        new Vector3Int(0,0,0),
        new Vector3Int(1,0,0),
        new Vector3Int(0,1,0),
        new Vector3Int(1,1,0),
        new Vector3Int(0,0,1),
        new Vector3Int(1,0,1),
        new Vector3Int(0,1,1),
        new Vector3Int(1,1,1)
    };

    private static readonly int[][] TriangleTable = new int[][] {
        new int[] { -1 },
        new int[] { 0, 3, 8, -1 },
        new int[] { 0, 9, 1, -1 },
        new int[] { 3, 8, 1, 1, 8, 9, -1 },
        new int[] { 2, 11, 3, -1 },
        new int[] { 8, 0, 11, 11, 0, 2, -1 },
        new int[] { 3, 2, 11, 1, 0, 9, -1 },
        new int[] { 11, 1, 2, 11, 9, 1, 11, 8, 9, -1 },
        new int[] { 1, 10, 2, -1 },
        new int[] { 0, 3, 8, 2, 1, 10, -1 },
        new int[] { 10, 2, 9, 9, 2, 0, -1 },
        new int[] { 8, 2, 3, 8, 10, 2, 8, 9, 10, -1 },
        new int[] { 11, 3, 10, 10, 3, 1, -1 },
        new int[] { 10, 0, 1, 10, 8, 0, 10, 11, 8, -1 },
        new int[] { 9, 3, 0, 9, 11, 3, 9, 10, 11, -1 },
        new int[] { 8, 9, 11, 11, 9, 10, -1 },
        new int[] { 4, 8, 7, -1 },
        new int[] { 7, 4, 3, 3, 4, 0, -1 },
        new int[] { 4, 8, 7, 0, 9, 1, -1 },
        new int[] { 1, 4, 9, 1, 7, 4, 1, 3, 7, -1 },
        new int[] { 8, 7, 4, 11, 3, 2, -1 },
        new int[] { 4, 11, 7, 4, 2, 11, 4, 0, 2, -1 },
        new int[] { 0, 9, 1, 8, 7, 4, 11, 3, 2, -1 },
        new int[] { 7, 4, 11, 11, 4, 2, 2, 4, 9, 2, 9, 1, -1 },
        new int[] { 4, 8, 7, 2, 1, 10, -1 },
        new int[] { 7, 4, 3, 3, 4, 0, 10, 2, 1, -1 },
        new int[] { 10, 2, 9, 9, 2, 0, 7, 4, 8, -1 },
        new int[] { 10, 2, 3, 10, 3, 4, 3, 7, 4, 9, 10, 4, -1 },
        new int[] { 1, 10, 3, 3, 10, 11, 4, 8, 7, -1 },
        new int[] { 10, 11, 1, 11, 7, 4, 1, 11, 4, 1, 4, 0, -1 },
        new int[] { 7, 4, 8, 9, 3, 0, 9, 11, 3, 9, 10, 11, -1 },
        new int[] { 7, 4, 11, 4, 9, 11, 9, 10, 11, -1 },
        new int[] { 9, 4, 5, -1 },
        new int[] { 9, 4, 5, 8, 0, 3, -1 },
        new int[] { 4, 5, 0, 0, 5, 1, -1 },
        new int[] { 5, 8, 4, 5, 3, 8, 5, 1, 3, -1 },
        new int[] { 9, 4, 5, 11, 3, 2, -1 },
        new int[] { 2, 11, 0, 0, 11, 8, 5, 9, 4, -1 },
        new int[] { 4, 5, 0, 0, 5, 1, 11, 3, 2, -1 },
        new int[] { 5, 1, 4, 1, 2, 11, 4, 1, 11, 4, 11, 8, -1 },
        new int[] { 1, 10, 2, 5, 9, 4, -1 },
        new int[] { 9, 4, 5, 0, 3, 8, 2, 1, 10, -1 },
        new int[] { 2, 5, 10, 2, 4, 5, 2, 0, 4, -1 },
        new int[] { 10, 2, 5, 5, 2, 4, 4, 2, 3, 4, 3, 8, -1 },
        new int[] { 11, 3, 10, 10, 3, 1, 4, 5, 9, -1 },
        new int[] { 4, 5, 9, 10, 0, 1, 10, 8, 0, 10, 11, 8, -1 },
        new int[] { 11, 3, 0, 11, 0, 5, 0, 4, 5, 10, 11, 5, -1 },
        new int[] { 4, 5, 8, 5, 10, 8, 10, 11, 8, -1 },
        new int[] { 8, 7, 9, 9, 7, 5, -1 },
        new int[] { 3, 9, 0, 3, 5, 9, 3, 7, 5, -1 },
        new int[] { 7, 0, 8, 7, 1, 0, 7, 5, 1, -1 },
        new int[] { 7, 5, 3, 3, 5, 1, -1 },
        new int[] { 5, 9, 7, 7, 9, 8, 2, 11, 3, -1 },
        new int[] { 2, 11, 7, 2, 7, 9, 7, 5, 9, 0, 2, 9, -1 },
        new int[] { 2, 11, 3, 7, 0, 8, 7, 1, 0, 7, 5, 1, -1 },
        new int[] { 2, 11, 1, 11, 7, 1, 7, 5, 1, -1 },
        new int[] { 8, 7, 9, 9, 7, 5, 2, 1, 10, -1 },
        new int[] { 10, 2, 1, 3, 9, 0, 3, 5, 9, 3, 7, 5, -1 },
        new int[] { 7, 5, 8, 5, 10, 2, 8, 5, 2, 8, 2, 0, -1 },
        new int[] { 10, 2, 5, 2, 3, 5, 3, 7, 5, -1 },
        new int[] { 8, 7, 5, 8, 5, 9, 11, 3, 10, 3, 1, 10, -1 },
        new int[] { 5, 11, 7, 10, 11, 5, 1, 9, 0, -1 },
        new int[] { 11, 5, 10, 7, 5, 11, 8, 3, 0, -1 },
        new int[] { 5, 11, 7, 10, 11, 5, -1 },
        new int[] { 6, 7, 11, -1 },
        new int[] { 7, 11, 6, 3, 8, 0, -1 },
        new int[] { 6, 7, 11, 0, 9, 1, -1 },
        new int[] { 9, 1, 8, 8, 1, 3, 6, 7, 11, -1 },
        new int[] { 3, 2, 7, 7, 2, 6, -1 },
        new int[] { 0, 7, 8, 0, 6, 7, 0, 2, 6, -1 },
        new int[] { 6, 7, 2, 2, 7, 3, 9, 1, 0, -1 },
        new int[] { 6, 7, 8, 6, 8, 1, 8, 9, 1, 2, 6, 1, -1 },
        new int[] { 11, 6, 7, 10, 2, 1, -1 },
        new int[] { 3, 8, 0, 11, 6, 7, 10, 2, 1, -1 },
        new int[] { 0, 9, 2, 2, 9, 10, 7, 11, 6, -1 },
        new int[] { 6, 7, 11, 8, 2, 3, 8, 10, 2, 8, 9, 10, -1 },
        new int[] { 7, 10, 6, 7, 1, 10, 7, 3, 1, -1 },
        new int[] { 8, 0, 7, 7, 0, 6, 6, 0, 1, 6, 1, 10, -1 },
        new int[] { 7, 3, 6, 3, 0, 9, 6, 3, 9, 6, 9, 10, -1 },
        new int[] { 6, 7, 10, 7, 8, 10, 8, 9, 10, -1 },
        new int[] { 11, 6, 8, 8, 6, 4, -1 },
        new int[] { 6, 3, 11, 6, 0, 3, 6, 4, 0, -1 },
        new int[] { 11, 6, 8, 8, 6, 4, 1, 0, 9, -1 },
        new int[] { 1, 3, 9, 3, 11, 6, 9, 3, 6, 9, 6, 4, -1 },
        new int[] { 2, 8, 3, 2, 4, 8, 2, 6, 4, -1 },
        new int[] { 4, 0, 6, 6, 0, 2, -1 },
        new int[] { 9, 1, 0, 2, 8, 3, 2, 4, 8, 2, 6, 4, -1 },
        new int[] { 9, 1, 4, 1, 2, 4, 2, 6, 4, -1 },
        new int[] { 4, 8, 6, 6, 8, 11, 1, 10, 2, -1 },
        new int[] { 1, 10, 2, 6, 3, 11, 6, 0, 3, 6, 4, 0, -1 },
        new int[] { 11, 6, 4, 11, 4, 8, 10, 2, 9, 2, 0, 9, -1 },
        new int[] { 10, 4, 9, 6, 4, 10, 11, 2, 3, -1 },
        new int[] { 4, 8, 3, 4, 3, 10, 3, 1, 10, 6, 4, 10, -1 },
        new int[] { 1, 10, 0, 10, 6, 0, 6, 4, 0, -1 },
        new int[] { 4, 10, 6, 9, 10, 4, 0, 8, 3, -1 },
        new int[] { 4, 10, 6, 9, 10, 4, -1 },
        new int[] { 6, 7, 11, 4, 5, 9, -1 },
        new int[] { 4, 5, 9, 7, 11, 6, 3, 8, 0, -1 },
        new int[] { 1, 0, 5, 5, 0, 4, 11, 6, 7, -1 },
        new int[] { 11, 6, 7, 5, 8, 4, 5, 3, 8, 5, 1, 3, -1 },
        new int[] { 3, 2, 7, 7, 2, 6, 9, 4, 5, -1 },
        new int[] { 5, 9, 4, 0, 7, 8, 0, 6, 7, 0, 2, 6, -1 },
        new int[] { 3, 2, 6, 3, 6, 7, 1, 0, 5, 0, 4, 5, -1 },
        new int[] { 6, 1, 2, 5, 1, 6, 4, 7, 8, -1 },
        new int[] { 10, 2, 1, 6, 7, 11, 4, 5, 9, -1 },
        new int[] { 0, 3, 8, 4, 5, 9, 11, 6, 7, 10, 2, 1, -1 },
        new int[] { 7, 11, 6, 2, 5, 10, 2, 4, 5, 2, 0, 4, -1 },
        new int[] { 8, 4, 7, 5, 10, 6, 3, 11, 2, -1 },
        new int[] { 9, 4, 5, 7, 10, 6, 7, 1, 10, 7, 3, 1, -1 },
        new int[] { 10, 6, 5, 7, 8, 4, 1, 9, 0, -1 },
        new int[] { 4, 3, 0, 7, 3, 4, 6, 5, 10, -1 },
        new int[] { 10, 6, 5, 8, 4, 7, -1 },
        new int[] { 9, 6, 5, 9, 11, 6, 9, 8, 11, -1 },
        new int[] { 11, 6, 3, 3, 6, 0, 0, 6, 5, 0, 5, 9, -1 },
        new int[] { 11, 6, 5, 11, 5, 0, 5, 1, 0, 8, 11, 0, -1 },
        new int[] { 11, 6, 3, 6, 5, 3, 5, 1, 3, -1 },
        new int[] { 9, 8, 5, 8, 3, 2, 5, 8, 2, 5, 2, 6, -1 },
        new int[] { 5, 9, 6, 9, 0, 6, 0, 2, 6, -1 },
        new int[] { 1, 6, 5, 2, 6, 1, 3, 0, 8, -1 },
        new int[] { 1, 6, 5, 2, 6, 1, -1 },
        new int[] { 2, 1, 10, 9, 6, 5, 9, 11, 6, 9, 8, 11, -1 },
        new int[] { 9, 0, 1, 3, 11, 2, 5, 10, 6, -1 },
        new int[] { 11, 0, 8, 2, 0, 11, 10, 6, 5, -1 },
        new int[] { 3, 11, 2, 5, 10, 6, -1 },
        new int[] { 1, 8, 3, 9, 8, 1, 5, 10, 6, -1 },
        new int[] { 6, 5, 10, 0, 1, 9, -1 },
        new int[] { 8, 3, 0, 5, 10, 6, -1 },
        new int[] { 6, 5, 10, -1 },
        new int[] { 10, 5, 6, -1 },
        new int[] { 0, 3, 8, 6, 10, 5, -1 },
        new int[] { 10, 5, 6, 9, 1, 0, -1 },
        new int[] { 3, 8, 1, 1, 8, 9, 6, 10, 5, -1 },
        new int[] { 2, 11, 3, 6, 10, 5, -1 },
        new int[] { 8, 0, 11, 11, 0, 2, 5, 6, 10, -1 },
        new int[] { 1, 0, 9, 2, 11, 3, 6, 10, 5, -1 },
        new int[] { 5, 6, 10, 11, 1, 2, 11, 9, 1, 11, 8, 9, -1 },
        new int[] { 5, 6, 1, 1, 6, 2, -1 },
        new int[] { 5, 6, 1, 1, 6, 2, 8, 0, 3, -1 },
        new int[] { 6, 9, 5, 6, 0, 9, 6, 2, 0, -1 },
        new int[] { 6, 2, 5, 2, 3, 8, 5, 2, 8, 5, 8, 9, -1 },
        new int[] { 3, 6, 11, 3, 5, 6, 3, 1, 5, -1 },
        new int[] { 8, 0, 1, 8, 1, 6, 1, 5, 6, 11, 8, 6, -1 },
        new int[] { 11, 3, 6, 6, 3, 5, 5, 3, 0, 5, 0, 9, -1 },
        new int[] { 5, 6, 9, 6, 11, 9, 11, 8, 9, -1 },
        new int[] { 5, 6, 10, 7, 4, 8, -1 },
        new int[] { 0, 3, 4, 4, 3, 7, 10, 5, 6, -1 },
        new int[] { 5, 6, 10, 4, 8, 7, 0, 9, 1, -1 },
        new int[] { 6, 10, 5, 1, 4, 9, 1, 7, 4, 1, 3, 7, -1 },
        new int[] { 7, 4, 8, 6, 10, 5, 2, 11, 3, -1 },
        new int[] { 10, 5, 6, 4, 11, 7, 4, 2, 11, 4, 0, 2, -1 },
        new int[] { 4, 8, 7, 6, 10, 5, 3, 2, 11, 1, 0, 9, -1 },
        new int[] { 1, 2, 10, 11, 7, 6, 9, 5, 4, -1 },
        new int[] { 2, 1, 6, 6, 1, 5, 8, 7, 4, -1 },
        new int[] { 0, 3, 7, 0, 7, 4, 2, 1, 6, 1, 5, 6, -1 },
        new int[] { 8, 7, 4, 6, 9, 5, 6, 0, 9, 6, 2, 0, -1 },
        new int[] { 7, 2, 3, 6, 2, 7, 5, 4, 9, -1 },
        new int[] { 4, 8, 7, 3, 6, 11, 3, 5, 6, 3, 1, 5, -1 },
        new int[] { 5, 0, 1, 4, 0, 5, 7, 6, 11, -1 },
        new int[] { 9, 5, 4, 6, 11, 7, 0, 8, 3, -1 },
        new int[] { 11, 7, 6, 9, 5, 4, -1 },
        new int[] { 6, 10, 4, 4, 10, 9, -1 },
        new int[] { 6, 10, 4, 4, 10, 9, 3, 8, 0, -1 },
        new int[] { 0, 10, 1, 0, 6, 10, 0, 4, 6, -1 },
        new int[] { 6, 10, 1, 6, 1, 8, 1, 3, 8, 4, 6, 8, -1 },
        new int[] { 9, 4, 10, 10, 4, 6, 3, 2, 11, -1 },
        new int[] { 2, 11, 8, 2, 8, 0, 6, 10, 4, 10, 9, 4, -1 },
        new int[] { 11, 3, 2, 0, 10, 1, 0, 6, 10, 0, 4, 6, -1 },
        new int[] { 6, 8, 4, 11, 8, 6, 2, 10, 1, -1 },
        new int[] { 4, 1, 9, 4, 2, 1, 4, 6, 2, -1 },
        new int[] { 3, 8, 0, 4, 1, 9, 4, 2, 1, 4, 6, 2, -1 },
        new int[] { 6, 2, 4, 4, 2, 0, -1 },
        new int[] { 3, 8, 2, 8, 4, 2, 4, 6, 2, -1 },
        new int[] { 4, 6, 9, 6, 11, 3, 9, 6, 3, 9, 3, 1, -1 },
        new int[] { 8, 6, 11, 4, 6, 8, 9, 0, 1, -1 },
        new int[] { 11, 3, 6, 3, 0, 6, 0, 4, 6, -1 },
        new int[] { 8, 6, 11, 4, 6, 8, -1 },
        new int[] { 10, 7, 6, 10, 8, 7, 10, 9, 8, -1 },
        new int[] { 3, 7, 0, 7, 6, 10, 0, 7, 10, 0, 10, 9, -1 },
        new int[] { 6, 10, 7, 7, 10, 8, 8, 10, 1, 8, 1, 0, -1 },
        new int[] { 6, 10, 7, 10, 1, 7, 1, 3, 7, -1 },
        new int[] { 3, 2, 11, 10, 7, 6, 10, 8, 7, 10, 9, 8, -1 },
        new int[] { 2, 9, 0, 10, 9, 2, 6, 11, 7, -1 },
        new int[] { 0, 8, 3, 7, 6, 11, 1, 2, 10, -1 },
        new int[] { 7, 6, 11, 1, 2, 10, -1 },
        new int[] { 2, 1, 9, 2, 9, 7, 9, 8, 7, 6, 2, 7, -1 },
        new int[] { 2, 7, 6, 3, 7, 2, 0, 1, 9, -1 },
        new int[] { 8, 7, 0, 7, 6, 0, 6, 2, 0, -1 },
        new int[] { 7, 2, 3, 6, 2, 7, -1 },
        new int[] { 8, 1, 9, 3, 1, 8, 11, 7, 6, -1 },
        new int[] { 11, 7, 6, 1, 9, 0, -1 },
        new int[] { 6, 11, 7, 0, 8, 3, -1 },
        new int[] { 11, 7, 6, -1 },
        new int[] { 7, 11, 5, 5, 11, 10, -1 },
        new int[] { 10, 5, 11, 11, 5, 7, 0, 3, 8, -1 },
        new int[] { 7, 11, 5, 5, 11, 10, 0, 9, 1, -1 },
        new int[] { 7, 11, 10, 7, 10, 5, 3, 8, 1, 8, 9, 1, -1 },
        new int[] { 5, 2, 10, 5, 3, 2, 5, 7, 3, -1 },
        new int[] { 5, 7, 10, 7, 8, 0, 10, 7, 0, 10, 0, 2, -1 },
        new int[] { 0, 9, 1, 5, 2, 10, 5, 3, 2, 5, 7, 3, -1 },
        new int[] { 9, 7, 8, 5, 7, 9, 10, 1, 2, -1 },
        new int[] { 1, 11, 2, 1, 7, 11, 1, 5, 7, -1 },
        new int[] { 8, 0, 3, 1, 11, 2, 1, 7, 11, 1, 5, 7, -1 },
        new int[] { 7, 11, 2, 7, 2, 9, 2, 0, 9, 5, 7, 9, -1 },
        new int[] { 7, 9, 5, 8, 9, 7, 3, 11, 2, -1 },
        new int[] { 3, 1, 7, 7, 1, 5, -1 },
        new int[] { 8, 0, 7, 0, 1, 7, 1, 5, 7, -1 },
        new int[] { 0, 9, 3, 9, 5, 3, 5, 7, 3, -1 },
        new int[] { 9, 7, 8, 5, 7, 9, -1 },
        new int[] { 8, 5, 4, 8, 10, 5, 8, 11, 10, -1 },
        new int[] { 0, 3, 11, 0, 11, 5, 11, 10, 5, 4, 0, 5, -1 },
        new int[] { 1, 0, 9, 8, 5, 4, 8, 10, 5, 8, 11, 10, -1 },
        new int[] { 10, 3, 11, 1, 3, 10, 9, 5, 4, -1 },
        new int[] { 3, 2, 8, 8, 2, 4, 4, 2, 10, 4, 10, 5, -1 },
        new int[] { 10, 5, 2, 5, 4, 2, 4, 0, 2, -1 },
        new int[] { 5, 4, 9, 8, 3, 0, 10, 1, 2, -1 },
        new int[] { 2, 10, 1, 4, 9, 5, -1 },
        new int[] { 8, 11, 4, 11, 2, 1, 4, 11, 1, 4, 1, 5, -1 },
        new int[] { 0, 5, 4, 1, 5, 0, 2, 3, 11, -1 },
        new int[] { 0, 11, 2, 8, 11, 0, 4, 9, 5, -1 },
        new int[] { 5, 4, 9, 2, 3, 11, -1 },
        new int[] { 4, 8, 5, 8, 3, 5, 3, 1, 5, -1 },
        new int[] { 0, 5, 4, 1, 5, 0, -1 },
        new int[] { 5, 4, 9, 3, 0, 8, -1 },
        new int[] { 5, 4, 9, -1 },
        new int[] { 11, 4, 7, 11, 9, 4, 11, 10, 9, -1 },
        new int[] { 0, 3, 8, 11, 4, 7, 11, 9, 4, 11, 10, 9, -1 },
        new int[] { 11, 10, 7, 10, 1, 0, 7, 10, 0, 7, 0, 4, -1 },
        new int[] { 3, 10, 1, 11, 10, 3, 7, 8, 4, -1 },
        new int[] { 3, 2, 10, 3, 10, 4, 10, 9, 4, 7, 3, 4, -1 },
        new int[] { 9, 2, 10, 0, 2, 9, 8, 4, 7, -1 },
        new int[] { 3, 4, 7, 0, 4, 3, 1, 2, 10, -1 },
        new int[] { 7, 8, 4, 10, 1, 2, -1 },
        new int[] { 7, 11, 4, 4, 11, 9, 9, 11, 2, 9, 2, 1, -1 },
        new int[] { 1, 9, 0, 4, 7, 8, 2, 3, 11, -1 },
        new int[] { 7, 11, 4, 11, 2, 4, 2, 0, 4, -1 },
        new int[] { 4, 7, 8, 2, 3, 11, -1 },
        new int[] { 9, 4, 1, 4, 7, 1, 7, 3, 1, -1 },
        new int[] { 7, 8, 4, 1, 9, 0, -1 },
        new int[] { 3, 4, 7, 0, 4, 3, -1 },
        new int[] { 7, 8, 4, -1 },
        new int[] { 11, 10, 8, 8, 10, 9, -1 },
        new int[] { 0, 3, 9, 3, 11, 9, 11, 10, 9, -1 },
        new int[] { 1, 0, 10, 0, 8, 10, 8, 11, 10, -1 },
        new int[] { 10, 3, 11, 1, 3, 10, -1 },
        new int[] { 3, 2, 8, 2, 10, 8, 10, 9, 8, -1 },
        new int[] { 9, 2, 10, 0, 2, 9, -1 },
        new int[] { 8, 3, 0, 10, 1, 2, -1 },
        new int[] { 2, 10, 1, -1 },
        new int[] { 2, 1, 11, 1, 9, 11, 9, 8, 11, -1 },
        new int[] { 11, 2, 3, 9, 0, 1, -1 },
        new int[] { 11, 0, 8, 2, 0, 11, -1 },
        new int[] { 3, 11, 2, -1 },
        new int[] { 1, 8, 3, 9, 8, 1, -1 },
        new int[] { 1, 9, 0, -1 },
        new int[] { 8, 3, 0, -1 },
        new int[] { -1 },
    };

    #endregion
}
