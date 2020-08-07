using System;
using UnityEngine;

namespace UnityVoxelCommunityProject.Legacy
{
    public class SettingsHolder : Singleton<SettingsHolder>
    {
        public GameObject player;
    
        [Space(10)]
        public CurrentGenerationSettings currentGenerationSettings;
    }
}