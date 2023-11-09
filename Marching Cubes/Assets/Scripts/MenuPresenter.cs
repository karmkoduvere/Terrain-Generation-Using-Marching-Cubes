
using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuPresenter : MonoBehaviour
{
    public Button NoiseSelectorPrefab;
    public GameObject Menu;
    public GameObject Grid;
    public List<NoiseSelectorData> NoiseGenerators;

    private void Awake()
    {
        for (int i = 0; i < Grid.transform.childCount; i++)
        {
            Destroy(Grid.transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < NoiseGenerators.Count; i++)
        {
            Button button = Instantiate<Button>(NoiseSelectorPrefab, Grid.transform);
            NoiseSelectorData noiseSelectorData = NoiseGenerators[i];
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = noiseSelectorData.MenuName;
            button.onClick.AddListener(() => SetTerrainNoiseGenerator(noiseSelectorData.NoiseGenerator));
        }
        Menu.SetActive(false);
    }

    private void SetTerrainNoiseGenerator(NoiseGenerator noiseGenerator)
    {
        MarchingCubes.Instance.NoiseGenerator = noiseGenerator;
        MarchingCubes.Instance.Start();
        Menu.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Menu.SetActive(! Menu.activeSelf);
        }
    }
}
