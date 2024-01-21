using System.Collections.Generic;
using UnityEngine;

public class InfiniteWorldGeneration : MonoBehaviour
{
    public bool enable = false;
    public Vector3Int size;

    private Vector3 playerPos;
    private Dictionary<Vector3Int, MarchingCubesGPU> chunkMap;
    private GameObject chunks;
    private Vector3Int corner;
    private int chunkSize;
    private MarchingCubesGPU ChunkGPUPrefab;

    private void Awake()
    {
        chunkMap = new();
        chunks = new("ChunksGPU");
        chunks.transform.parent = transform;
    }

    private void Start()
    {
        chunkSize = WorldGenerator.Instance.chunkSize;
        ChunkGPUPrefab = WorldGenerator.Instance.ChunkGPUPrefab;
        WorldGenerator.Instance.GenerateEdges = false;
        playerPos = Camera.main.transform.position;
        corner = CalcCorner();
        if (enable) Generate();
    }

    void Update()
    {
        if (enable)
        {
            playerPos = Camera.main.transform.position;
            Vector3Int newCorner = CalcCorner();
            if (corner != newCorner)
            {
                corner = newCorner;
                Generate();
            }
        }   
    }

    private Vector3Int CalcCorner()
    {
        int cornerDistancex = chunkSize * (size.x / 2);
        int cornerDistancez = chunkSize * (size.z / 2);
        if (playerPos.x < 0) cornerDistancex += chunkSize;
        if (playerPos.z < 0) cornerDistancez += chunkSize;
        int x = (int)(playerPos.x - cornerDistancex - playerPos.x % chunkSize);
        int z = (int)(playerPos.z - cornerDistancez - playerPos.z % chunkSize);
        return new Vector3Int(x, 0, z);
    }

    void Generate()
    {
        for (int z = 0; z < size.z; z++)
        {
            int chunkCoordz = corner.z + z * chunkSize;
            for (int y = 0; y < size.y; y++)
            {
                int chunkCoordy = corner.y + y * chunkSize;
                for (int x = 0; x < size.x; x++)
                {
                    int chunkCoordx = corner.x + x * chunkSize;
                    if (chunkMap.ContainsKey(new Vector3Int(chunkCoordx, chunkCoordy, chunkCoordz))) continue;
                    MarchingCubesGPU chunk = Instantiate(
                    ChunkGPUPrefab,
                        new Vector3(chunkCoordx, chunkCoordy, chunkCoordz),
                        Quaternion.identity,
                        chunks.transform
                    );
                    chunk.name = $"Chunk_({chunkCoordx}, {chunkCoordy}, {chunkCoordz})";
                    chunk.Generate(new Vector3(chunkCoordx / chunkSize, chunkCoordy / chunkSize, chunkCoordz / chunkSize));
                    chunkMap.Add(new Vector3Int(chunkCoordx, chunkCoordy, chunkCoordz), chunk);
                }
            }
        }
    }
}
