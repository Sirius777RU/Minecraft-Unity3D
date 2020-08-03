using System;
using UnityEngine;

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
            
        }

        private void OnApplicationQuit()
        {
            Debug.Log("Quit.");
        }
    }
}