using UnityEngine;

namespace UnityVoxelCommunityProject.General
{
    [CreateAssetMenu(fileName = "Display Options", menuName = "Settings/Display Options", order = 0)]
    public class DisplayOptions : ScriptableObject
    {
        [Range(4, 128)] public int chunkRenderDistance = 16;
    }
}