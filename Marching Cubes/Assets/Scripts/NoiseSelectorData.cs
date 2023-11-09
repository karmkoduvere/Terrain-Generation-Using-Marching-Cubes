using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/NoiseSelectorData")]
public class NoiseSelectorData : ScriptableObject
{
    public NoiseGenerator NoiseGenerator;
    public string MenuName;
}
