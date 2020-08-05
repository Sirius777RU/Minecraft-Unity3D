using System;
using Unity.Mathematics;
using UnityCommunityVoxelProject.Terrain;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCommunityVoxelProject.General
{
    public class GameSessionInitializer : Singleton<GameSessionInitializer>
    {
        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            //TerrainProceduralGeneration.Instance.GenerateChunk();
        }

        private void Update()
        {
            
        }

        private void OnApplicationQuit()
        {
            Debug.Log("Quit.");
        }
    }
}