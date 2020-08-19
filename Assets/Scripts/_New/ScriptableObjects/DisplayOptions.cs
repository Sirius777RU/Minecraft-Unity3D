using System;
using UnityEngine;

namespace UnityVoxelCommunityProject.General
{
    [CreateAssetMenu(fileName = "Display Options", menuName = "Settings/Display Options", order = 0)]
    public class DisplayOptions : ScriptableObject
    {
        [Range(0, 12)] public int renderDistance  = 8;
        [Range(0, 1)]  public int instantDistance = 1;
    }
}