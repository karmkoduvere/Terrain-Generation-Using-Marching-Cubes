using DefaultNamespace;
using Unity.VisualScripting;
using UnityEngine;

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
    
    public bool RandomGeneration = false;
    [HideInInspector]
    public Vector3 randomNoiseOffsetVector;


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
        if (Input.GetKeyDown(KeyCode.F)) RandomGeneration = !RandomGeneration;
        if (Input.GetKeyDown(KeyCode.R)) Refresh();
    }

    void Refresh()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        if (RandomGeneration)
        {
            randomNoiseOffsetVector = new Vector3(Random.Range(0, WorldSize.x),
                                                Random.Range(0, WorldSize.y),
                                                Random.Range(0, WorldSize.z)) * 4;
        }
        else
        {
            randomNoiseOffsetVector = Vector3.zero;
        }
        Generate();
    }

    void Generate()
    {
        GameObject chunks = new("Chunks");
        chunks.transform.parent = transform;

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
                        chunks.transform
                    );
                    chunk.name = $"Chunk_({x}, {y}, {z})";
                    chunk.Generate(new Vector3(x, y, z));
                }
            }
        }
    }
}
