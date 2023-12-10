using DefaultNamespace;
using Unity.VisualScripting;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator Instance;

    public Vector3Int WorldSize;
    [Range(0f, 1f)] public float PerlinDensity;
    [Range(0f, 1f)] public float NoiseCutOff;
    public readonly int chunkSize = 24;

    public MarchingCubes ChunkPrefab;
    public MarchingCubesGPU ChunkGPUPrefab;
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
        //Generate();
        //GenerateOnGPU();

        
        float sum1 = 0;
        float sum2 = 0;
        float time;
        GameObject temp;
        for (int i = 0; i < 5; i++)
        {
            /*
            time = Time.realtimeSinceStartup;
            temp = Generate();
            sum1 += (Time.realtimeSinceStartup - time);
            Destroy(temp);*/
            time = Time.realtimeSinceStartup;
            temp = GenerateOnGPU();
            sum2 += (Time.realtimeSinceStartup - time);
            if (i+1!=5)Destroy(temp);
        }
        print("createNoise: " + MarchingCubesGPU.num1/5);
        print("Dispatch: " + MarchingCubesGPU.num2 / 5);
        print("ReadData: " + MarchingCubesGPU.num3 / 5);
        print("data to list: " + MarchingCubesGPU.num4 / 5);
        print("data to mesh: " + MarchingCubesGPU.num5 / 5);

        print(MarchingCubesGPU.num1 / 5 + MarchingCubesGPU.num2 / 5 + MarchingCubesGPU.num3 / 5 + MarchingCubesGPU.num4 / 5 + MarchingCubesGPU.num5 / 5);

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
}
