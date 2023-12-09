using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class VariablesPanel : MonoBehaviour
{
    public UnityEngine.UI.Slider perlinDensity;

    public void Start()
    {
        perlinDensity.value = WorldGenerator.Instance.PerlinDensity;
        perlinDensity.onValueChanged.AddListener(delegate { SetPerlinDensity(); });
    }

    public void SetPerlinDensity()
    {
        WorldGenerator.Instance.PerlinDensity = perlinDensity.value;
    }
}
