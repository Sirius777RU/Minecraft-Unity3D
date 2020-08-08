using UnityEngine;

[CreateAssetMenu(fileName = "Generation Settings", menuName = "Settings/Game Settings", order = 0)]
public class CurrentGenerationSettings : ScriptableObject
{
    [Range(8, 64)] public int chunkWidth  = 16;
    [Range(8, 256)] public int chunkHeight = 64;
    [Range(0, 256)] public int seaLevel    = 28;
}