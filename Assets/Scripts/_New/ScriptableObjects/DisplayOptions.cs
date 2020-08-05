using UnityEngine;

namespace UnityCommunityVoxelProject.General
{
    [CreateAssetMenu(fileName = "DisplayOptions", menuName = "Settings/DisplayOptions", order = 0)]
    public class DisplayOptions : ScriptableObject
    {
        [Range(4, 128)] public int chunkRenderDistance = 16;
    }
}