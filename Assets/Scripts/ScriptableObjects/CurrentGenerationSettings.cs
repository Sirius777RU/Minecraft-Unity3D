using UnityEngine;

[CreateAssetMenu(fileName = "Generation Settings", menuName = "Settings/Game Settings", order = 0)]
public class CurrentGenerationSettings : ScriptableObject
{
    [Range(0, 512)] public int chunkHeight = 64;
    public int seaLevel = 28;
}