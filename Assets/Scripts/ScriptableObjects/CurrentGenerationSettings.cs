using UnityEngine;

[CreateAssetMenu(fileName = "Generation Settings", menuName = "Settings/Game Settings", order = 0)]
public class CurrentGenerationSettings : ScriptableObject
{
    [Range(8, 64)] public int chunkWidth  = 16;
    [Range(8, 256)] public int chunkHeight = 64;
    [Range(0, 256)] public int seaLevel    = 28;

    //Make sure numbers are even.
    private void OnValidate()
    {
        if (chunkWidth % 2 != 0)
        {
            chunkWidth++;
        }
        
        if (chunkHeight % 2 != 0)
        {
            chunkHeight++;
        }
    }
}