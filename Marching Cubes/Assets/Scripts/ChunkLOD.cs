using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ChunkLOD : MonoBehaviour
{
    public GameObject[] LODChuncks = new GameObject[4]; // Array of meshes for different LOD levels
    public float[] lodDistances = new float[] { 50, 100, 150, 200 }; // Array of distances for LOD transitions
    public LODGroup group;

    void createLODGroup()
    {
        // Programmatically create a LOD group and add LOD levels.
        // Create a GUI that allows for forcing a specific LOD level.
        group = gameObject.AddComponent<LODGroup>();

        // Add 4 LOD levels
        LOD[] lods = new LOD[4];
        for (int i = 0; i < 4; i++)
        {
            LODChuncks[i].transform.parent = gameObject.transform;
            Renderer[] renderers = new Renderer[1];
            renderers[0] = LODChuncks[i].GetComponent<MeshRenderer>();
            lods[i] = new LOD(lodDistances[i], renderers);
        }
        group.SetLODs(lods);
        group.RecalculateBounds();
    }
    
    /*
    public Mesh[] LODmeshes = new Mesh[4];
    public float[] LODranges = new float[] { 50, 100, 150 };

    private MeshRenderer m_Renderer;
    private bool inRange;
    private float lastRange;


    void Start()
    {
        m_Renderer = GetComponent<MeshRenderer>();
        //m_Renderer.enabled = false;
    }

    private void Update()
    {
        LODGroup a = gameObject.AddComponent<LODGroup>();
        a.set
        if (inRange)
        {
            float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
            
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        inRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        inRange = false;
    }

    */

}
