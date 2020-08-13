using System;
using UnityEngine;

namespace UnityVoxelCommunityProject.General
{
    [CreateAssetMenu(fileName = "Display Options", menuName = "Settings/Display Options", order = 0)]
    public class DisplayOptions : ScriptableObject
    {
        [Range(1, 12)] public int chunkRenderDistance = 8;
    }
}