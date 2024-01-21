using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class MarchingCubes : MonoBehaviour
{
    public GameObject ChunkLODPrefab;
    public GameObject Marker;
    public bool Mark;

    private Vector3Int worldSize;
    private int chunkSize;
    private Material material;

    private float[,,] noisePoints;

    private float[] LODTrainsitionDistances = new float[] { 0.4f, 0.2f, 0.1f, 0.05f }; // Array of distances for LOD transitions
    private LODGroup group;

    private List<Vector3> newVertices = new();
    private List<int> newTriangles = new();
    private List<Color> colors = new();
    private Dictionary<Vector3, int> verticeMap = new();

    public void Generate(Vector3 offset)
    {
        worldSize = WorldGenerator.Instance.WorldSize;
        chunkSize = WorldGenerator.Instance.chunkSize;
        material = WorldGenerator.Instance.Material;
        group = gameObject.GetComponent<LODGroup>();

        noisePoints = CreateNoisePoints(offset);

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
            GenerateChunk(offset, (i+1),chunkLOD);
            lods[i] = new LOD(LODTrainsitionDistances[i], new Renderer[] { chunkLOD.GetComponent<MeshRenderer>() });            
        }

        group.SetLODs(lods);
        group.RecalculateBounds();
    }

    public void GenerateChunk(Vector3 offset, int LOD, GameObject chunk)
    {
        // "Marches" over the chunk, at every point gets TriangeTable index based on
        // the generated points and then adds new vertices and triangles to the list.
        int step = 0;
        for (int z = 0; z < chunkSize; z += LOD)
        {
            for (int y = 0; y < chunkSize; y += LOD)
            {
                for (int x = 0; x < chunkSize; x += LOD)
                {
                    print(noisePoints[x, y, z]);
                    int index = GetTriangleIndex(new Vector3Int(x, y, z), LOD);
                    foreach (int el in Tables.TriangleTable[index])
                    {
                        if (el == -1) break;
                        Vector3 vertex = new Vector3(x, y, z) + Tables.cubeEdgeOffset[el] * LOD;

                        if (!verticeMap.ContainsKey(vertex))
                        {
                            //if (Mark) Instantiate(Marker, new Vector3(x, y, z) + CalcVertexPos(x, y, z, el, LOD) * LOD, Quaternion.identity, chunk.transform);
                            if (Mark) Instantiate(Marker, new Vector3(x, y, z) + Tables.cubeEdgeOffset[el] * LOD, Quaternion.identity, chunk.transform);

                            newVertices.Add(new Vector3(x, y, z) + CalcVertexPos(new Vector3Int(x,y,z), el,LOD)*LOD);
                            //newVertices.Add(new Vector3(x, y, z) + Tables.cubeEdgeOffset[el]*LOD); // Sharp edge version
                            verticeMap[vertex] = step++;
                            float colorValueX = (vertex.x + chunkSize * offset.x) / (worldSize.x * chunkSize);
                            float colorValueY = (vertex.y + chunkSize * offset.y) / (worldSize.y * chunkSize);
                            float colorValueZ = (vertex.z + chunkSize * offset.z) / (worldSize.z * chunkSize);
                            colors.Add(Color.HSVToRGB(colorValueX / 2 + colorValueZ / 2, colorValueY * 1 / 3 + 0.25f, colorValueY));
                        }

                        newTriangles.Add(verticeMap[vertex]);
                    }
                }
            }
        }

        Mesh mesh = new();

        mesh.SetVertices(newVertices);
        mesh.SetTriangles(newTriangles, 0, true);
        mesh.SetColors(colors);

        mesh.RecalculateNormals();
        chunk.GetComponent<MeshFilter>().mesh = mesh;
        chunk.GetComponent<MeshRenderer>().material = material;

        newVertices.Clear();
        newTriangles.Clear();
        colors.Clear();
        verticeMap.Clear();
    }

    public float[,,] CreateNoisePoints(Vector3 offset)
    {
        NativeArray<float> points = new((chunkSize + 1) * (chunkSize + 1) * (chunkSize + 1), Allocator.TempJob);

        GenerateNoiseJob job = new GenerateNoiseJob
        {
            chunkSize = chunkSize,
            worldSize = worldSize,
            xOffset = chunkSize * (int)offset.x,
            yOffset = chunkSize * (int)offset.y,
            zOffset = chunkSize * (int)offset.z,
            noiseGenerator = WorldGenerator.Instance.NoiseGenerator,
            points = points,
            noiseFrequency = WorldGenerator.Instance.NoiseScale / 100f
        };

        JobHandle jobHandle = job.Schedule(points.Length, 64);

        jobHandle.Complete();

        float[,,] points3D = new float[chunkSize + 1, chunkSize + 1, chunkSize + 1];

        for (int z = 0; z < chunkSize + 1; z++)
        {
            for (int y = 0; y < chunkSize + 1; y++)
            {
                for (int x = 0; x < chunkSize + 1; x++)
                {
                    int index = x + (chunkSize + 1) * (y + (chunkSize + 1) * z);
                    points3D[z, y, x] = points[index];
                }
            }
        }

        points.Dispose();

        return points3D;
    }

    Vector3 CalcVertexPos(Vector3Int coords, int n, int LOD)
    {
        Vector3Int pointA = coords + Tables.vertices[Tables.edgeVertexIndices[n][0]] * LOD;
        Vector3Int pointB = coords + Tables.vertices[Tables.edgeVertexIndices[n][1]] * LOD;

        float pos = (0.47f - noisePoints[pointA.z,pointA.y,pointA.x]) /
            (noisePoints[pointB.z, pointB.y, pointB.x] - noisePoints[pointA.z,pointA.y,pointA.x]);

        return n switch
        {
            0 or 2 or 4 or 6 => Tables.vertices[Tables.edgeVertexIndices[n][0]] + new Vector3(pos, 0, 0),
            1 or 3 or 5 or 7 => Tables.vertices[Tables.edgeVertexIndices[n][0]] + new Vector3(0, pos, 0),
            8 or 9 or 10 or 11 => Tables.vertices[Tables.edgeVertexIndices[n][0]] + new Vector3(0, 0, pos),
            _ => new Vector3(0, 0, 0)
        };
    }

    // Calculates index for the TriangleTable based on generated noisePoints and given coords
    int GetTriangleIndex(Vector3Int coords, int LOD)
    {
        int index = 0;
        for (int i = 0; i < 8; i++)
        {
            Vector3Int coord = coords + Tables.vertices[i] * LOD;
            if (!(noisePoints[coord.z, coord.y, coord.x] < 0.47f))
                index |= (1 << i);
        }
        return index;
    }
}
