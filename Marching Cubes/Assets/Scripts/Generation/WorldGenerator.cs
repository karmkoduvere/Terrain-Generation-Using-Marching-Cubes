using DefaultNamespace;
using Unity.VisualScripting;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator Instance;

    [Header("World")]
    public Vector3Int WorldSize;
    public readonly int chunkSize = 24;
    public MarchingCubes ChunkPrefab;
    public MarchingCubesGPU ChunkGPUPrefab;

    public Material Material;

    [Header("Noise")]
    public int Seed;
    [Tooltip("Samples of noise.")]
    public int Octaves;
    [Tooltip("How quickly the noise varies over space.")]
    public float NoiseScale;
    public float NoiseWeight;

    public float WeightMultiplier;

    public float FloorOffset;
    public float HardFloor;
    public float HardFloorWeight;

    [Header("Offset noise")]
    public float OffsetNoiseScale;
    public int OffsetOctaves;
    public float OffsetWeight;

    public NoiseGenerator NoiseGenerator = NoiseGenerator.Simplex3D;
    public OffsetNoiseGenerator OffsetNoiseGenerator = OffsetNoiseGenerator.OffsetType1;

    [HideInInspector] public Vector3 NoiseOffset;

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
        Random.InitState(Seed);
        int maxvalue = 10000;
        NoiseOffset = new Vector3(Random.value * maxvalue, Random.value * maxvalue, Random.value * maxvalue);

        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        float t = Time.realtimeSinceStartup;
        GenerateOnGPU();
        print(Time.realtimeSinceStartup - t);
    }

    GameObject Generate()
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
        return chunks;
    }

    GameObject GenerateOnGPU()
    {
        GameObject chunks = new("ChunksGPU");
        chunks.transform.parent = transform;

        for (int z = 0; z < WorldSize.z; z++)
        {
            for (int y = 0; y < WorldSize.y; y++)
            {
                for (int x = 0; x < WorldSize.x; x++)
                {
                    MarchingCubesGPU chunk = Instantiate(
                        ChunkGPUPrefab,
                        new Vector3(x * chunkSize, y * chunkSize, z * chunkSize),
                        Quaternion.identity,
                        chunks.transform
                    );
                    chunk.name = $"Chunk_({x}, {y}, {z})";
                    chunk.Generate(new Vector3(x, y, z));
                }
            }
        }
        return chunks;
    }
}
