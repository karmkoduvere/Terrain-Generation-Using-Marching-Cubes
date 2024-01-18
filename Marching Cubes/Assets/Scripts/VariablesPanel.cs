using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class VariablesPanel : MonoBehaviour
{
    public UnityEngine.UI.Slider NoiseScale;
    // Start is called before the first frame update
    public void Start()
    {
        NoiseScale.value = WorldGenerator.Instance.NoiseScale;
        NoiseScale.onValueChanged.AddListener(delegate { SetNoiseScale(); });
    }

    // Update is called once per frame
    public void Update()
    {

    }

    public void SetNoiseScale()
    {
        WorldGenerator.Instance.NoiseScale = (int)NoiseScale.value;
    }
}
