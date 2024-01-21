
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DefaultNamespace;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MenuPresenter : MonoBehaviour
{
    public static MenuPresenter Instance;
    
    public Button NoiseSelectorPrefab;
    public GameObject Menu;
    public GameObject Grid;
    public TextMeshProUGUI controlsText;
    public List<NoiseSelectorData> NoiseGenerators;

    private void Awake()
    {
        Instance = this;
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
        WorldGenerator.Instance.NoiseGenerator = noiseGenerator;
        InfiniteWorldGeneration.Instance.Refresh();
        Menu.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Menu.SetActive(! Menu.activeSelf);
        }
    }

    public void UpdateOffsetToggleText(bool offsetToggle)
    {
        string replacement = offsetToggle ? "ON" : "OFF";
        controlsText.text = Regex.Replace(controlsText.text, @"Generate random terrain toggle - F \(.*?\)",
            $"Generate random terrain toggle - F ({replacement})");
    }

    public void Exit()
    {
        Application.Quit();
    }
}
