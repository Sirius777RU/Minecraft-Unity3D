using UnityCommunityVoxelProject.General;
using UnityEngine;

namespace UnityCommunityVoxelProject.Utility
{
    public class SettingsHolder : Singleton<SettingsHolder>
    {
        public CurrentGenerationSettings proceduralGeneration;
        public DisplayOptions displayOptions;
    }
}