using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeGenerator : MonoBehaviour
{
    public static TreeGenerator Instance;
    public GameObject TreePrefab;
    public int TreeAmountPerChunk;
    public Vector3 TreeSize;
    public bool enable;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            enable = !enable;
        }
    }

    public void GenerateTrees(Vector3 offset, LODGroup lod)
    {
        if (enable)
        {
            int chunkSize = WorldGenerator.Instance.chunkSize;
            Debug.Log(offset);
            Vector3 position = new Vector3(offset.x * chunkSize + chunkSize / 2, offset.y + chunkSize, offset.z * chunkSize + chunkSize / 2);
            //cube.GetComponent<BoxCollider>().scale;
            for (int i = 0; i < TreeAmountPerChunk; i++)
            {
                Vector3 randomPointInCube = RandomPointInBounds(position, chunkSize);
                Ray ray = new Ray(randomPointInCube, new Vector3(0f, -300f, 0f));
                GameObject tree = Instantiate(TreePrefab, lod.transform);
                tree.transform.localScale = TreeSize;
                RaycastHit hitData;
                Physics.Raycast(ray, out hitData);
                if (hitData.point == Vector3.zero) Destroy(tree);
                else
                    tree.transform.position = hitData.point;
            }
        }
    }

    private Vector3 RandomPointInBounds(Vector3 position, int chunkSize)
    {
        return new Vector3(Random.Range(position.x - chunkSize/2, position.x + chunkSize/2),
            100f,
            Random.Range(position.z - chunkSize/2, position.z + chunkSize/2)
            );;
    }
}
