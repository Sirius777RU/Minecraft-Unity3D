using UnityEngine;
using UnityVoxelCommunityProject.General;

namespace UnityVoxelCommunityProject.Utility
{
    public class SettingsHolder : Singleton<SettingsHolder>
    {
        public CurrentGenerationSettings proceduralGeneration;
        public DisplayOptions displayOptions;
    }
}