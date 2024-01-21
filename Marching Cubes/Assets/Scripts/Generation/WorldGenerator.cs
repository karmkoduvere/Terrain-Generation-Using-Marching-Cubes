using DefaultNamespace;
using UnityEngine;
using UnityEngine.Rendering;

public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator Instance;

    [Header("World")]
    public Vector3Int WorldSize;
    public readonly int chunkSize = 24;
    public MarchingCubes ChunkPrefab;
    public MarchingCubesGPU ChunkGPUPrefab;
    public Material Material;
    public bool GenerateInfinite = false;
    public bool randomOffset = false;

    [Header("Color")]
    public Color Color1 = Color.blue;
    public Color Color2 = Color.yellow;
    public Color Color3 = Color.green;
    public Color Color4 = Color.gray;
    public Color Color5 = Color.white;

    public float colorCutoff1 = 0.2f;
    public float colorCutoff2 = 0.4f;
    public float colorCutoff3 = 0.6f;
    public float colorCutoff4 = 0.7f;


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

    [HideInInspector] public Vector3 NoiseOffset = Vector3.zero;

    private int initialSeed;

    private void Awake()
    {
        Instance = this;
        initialSeed = Seed;
    }

    public void Start()
    {
        SetSeed();
        MenuPresenter.Instance.UpdateOffsetToggleText(randomOffset);
        Refresh();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleRandomOffset();
            SetSeed();
        } else if (Input.GetKeyDown(KeyCode.R))
        {
            Refresh();
        } else if (Input.GetKeyDown(KeyCode.G))
        {
            GenerateInfinite = !GenerateInfinite;
            Refresh();
        }

        if (GenerateInfinite) InfiniteWorldGeneration.Instance.InfiniteWorldGenerate();
    }

    public void Refresh()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        if (GenerateInfinite)
        {
            InfiniteWorldGeneration.Instance.Refresh();
        }
        else
        {
            GenerateOnGPU();
        }
    }

    public void SetSeed()
    {
        if (!randomOffset)
        {
            Seed = initialSeed;
        }
        else
        {
            Random.InitState((int) Time.time);
            Seed = Random.Range(0, 1000);
        }
        CalculateNoiseOffset();
    }

    void CalculateNoiseOffset()
    {
        Random.InitState(Seed);
        int maxvalue = 10000;
        NoiseOffset = new Vector3(Random.value * maxvalue, Random.value * maxvalue, Random.value * maxvalue);
    }

    public void ToggleRandomOffset()
    {
        randomOffset = !randomOffset;
        MenuPresenter.Instance.UpdateOffsetToggleText(randomOffset);
    }
    /*
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
    }*/

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
