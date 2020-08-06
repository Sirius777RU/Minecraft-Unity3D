using System;
using Unity.Mathematics;
using UnityCommunityVoxelProject.Serialization;
using UnityCommunityVoxelProject.Terrain;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCommunityVoxelProject.Utility
{
    public class GameSessionInitializer : Singleton<GameSessionInitializer>
    {
        protected override void Awake()
        {
            
        }

        private void Start()
        {
           SaveLoadSystem.Instance.Initialize();
           ChunkManager.Instance.Initialize();
        }

        private void OnApplicationQuit()
        {
            Debug.Log("Quit.");
        }
    }
}