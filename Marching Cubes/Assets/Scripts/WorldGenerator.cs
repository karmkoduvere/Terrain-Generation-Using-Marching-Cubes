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
    public MarchingCubesGPUExperimental ChunkGPUExperimentalPrefab;

    public Material Material;

    [Header("Noise")]
    [Range(0, 100)] public int NoiseScale;
    [Range(0f, 1f)] public float NoiseCutOff;
    public NoiseGenerator NoiseGenerator = NoiseGenerator.PerlinNoise3D;

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
        
        //Generate();
        /*
        float sum1 = 0;
        for (int i = 0; i < 5; i++)
        {
            float time = Time.realtimeSinceStartup;
            GameObject temp = Generate();
            sum1 += (Time.realtimeSinceStartup - time);
            Destroy(temp);
        }
        print(sum1 / 5);
        /**/
        //GenerateOnGPUExperimental();

        //return;
        float sum1 = 0;
        float sum2 = 0;
        float time;
        GameObject temp;
        for (int i = 0; i < 5; i++)
        {
            
            time = Time.realtimeSinceStartup;
            temp = Generate();
            sum1 += (Time.realtimeSinceStartup - time);
            Destroy(temp);
            time = Time.realtimeSinceStartup;
            temp = GenerateOnGPUExperimental();
            sum2 += (Time.realtimeSinceStartup - time);
            Destroy(temp);
            //if (i+1!=5)Destroy(temp);
        }
        print("createNoise: " + MarchingCubesGPUExperimental.num1/5);
        print("Dispatch: " + MarchingCubesGPUExperimental.num2 / 5);
        print("ReadData: " + MarchingCubesGPUExperimental.num3 / 5);
        print("data to list: " + MarchingCubesGPUExperimental.num4 / 5);
        print("data to mesh: " + MarchingCubesGPUExperimental.num5 / 5);

        print(MarchingCubesGPUExperimental.num1 / 5 +
            MarchingCubesGPUExperimental.num2 / 5 + 
            MarchingCubesGPUExperimental.num3 / 5 + 
            MarchingCubesGPUExperimental.num4 / 5 + 
            MarchingCubesGPUExperimental.num5 / 5);

        print(" ");
        print("createNoise: " + MarchingCubes.num1 / 5);
        print("Marching: " + MarchingCubes.num2 / 5);
        print("Data to mesh: " + MarchingCubes.num3 / 5);

        print(" ");
        print("CPU: "+sum1 / 5);
        print("GPU: " +sum2 / 5);
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
    GameObject GenerateOnGPUExperimental()
    {
        GameObject chunks = new("ChunksGPU");
        chunks.transform.parent = transform;

        for (int z = 0; z < WorldSize.z; z++)
        {
            for (int y = 0; y < WorldSize.y; y++)
            {
                for (int x = 0; x < WorldSize.x; x++)
                {
                    MarchingCubesGPUExperimental chunk = Instantiate(
                        ChunkGPUExperimentalPrefab,
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
