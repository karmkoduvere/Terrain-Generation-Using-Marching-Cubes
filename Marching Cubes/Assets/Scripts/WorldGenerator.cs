using DefaultNamespace;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator Instance;

    public Vector3 WorldSize;
    [Range(0f, 1f)] public float PerlinDensity;
    [Range(0f, 1f)] public float NoiseCutOff;
    public readonly int chunkSize = 24;

    public MarchingCubes ChunkPrefab;
    public NoiseGenerator NoiseGenerator = NoiseGenerator.PerlinNoise3D;
    public Material Material;


    private void Awake()
    {
        Instance = this;
    }

    public void Start()
    {
        Refresh();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) Refresh();
    }

    void Refresh()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        Generate();
    }

    void Generate()
    {
        GameObject chunksParent = new("ChunksParent");
        chunksParent.transform.parent = transform;

        for (int z = 0; z < WorldSize.z; z++)
        {
            for (int y = 0; y < WorldSize.y; y++)
            {
                for (int x = 0; x < WorldSize.x; x++)
                {
                    MarchingCubes chunk = Instantiate(
                        ChunkPrefab,
                        new Vector3(x * chunkSize, y * chunkSize, z * chunkSize),
                        Quaternion.identity,
                        chunksParent.transform
                    );
                    chunk.Generate(new Vector3(x, y, z));
                }
            }
        }
    }
}
