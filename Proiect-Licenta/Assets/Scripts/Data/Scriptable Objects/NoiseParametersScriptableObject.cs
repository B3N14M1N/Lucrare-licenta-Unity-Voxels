using System;
using UnityEngine;

[CreateAssetMenu(fileName ="Noise Parameters", menuName ="ScriptableObjects/NoiseParameters")]
public class NoiseParametersScriptableObject : ScriptableObject
{
    [SerializeField]
    public NoiseParameters values;
};

[Serializable]
public struct NoiseParameters
{
    public float scale;
    public int octaves;
    public float frequency;
    public float lacunarity;
    public float persistence;
};