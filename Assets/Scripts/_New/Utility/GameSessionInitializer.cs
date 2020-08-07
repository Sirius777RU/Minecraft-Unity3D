using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityVoxelCommunityProject.Serialization;
using UnityVoxelCommunityProject.Terrain;

namespace UnityVoxelCommunityProject.Utility
{
    public class GameSessionInitializer : Singleton<GameSessionInitializer>
    {
        protected override void Awake()
        {
            base.Awake();
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