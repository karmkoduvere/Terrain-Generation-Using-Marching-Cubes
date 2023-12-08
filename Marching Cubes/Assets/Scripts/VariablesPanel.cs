using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class VariablesPanel : MonoBehaviour
{
    public UnityEngine.UI.Slider perlinDensity;
    // Start is called before the first frame update
    public void Start()
    {
        perlinDensity.value = WorldGenerator.Instance.PerlinDensity;
        perlinDensity.onValueChanged.AddListener(delegate { SetPerlinDensity(); });
    }

    // Update is called once per frame
    public void Update()
    {

    }

    public void SetPerlinDensity()
    {
        WorldGenerator.Instance.PerlinDensity = perlinDensity.value;
    }
}
