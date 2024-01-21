using UnityEngine;

public class VariablesPanel : MonoBehaviour
{
    public GameObject RuntimeHierarchy;
    public GameObject RuntimeInspector;
    // Start is called before the first frame update
    

    // Update is called once per frame
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) 
        { 
            RuntimeHierarchy.SetActive(!RuntimeHierarchy.activeSelf);
            RuntimeInspector.SetActive(!RuntimeInspector.activeSelf);
        }
    }
}
