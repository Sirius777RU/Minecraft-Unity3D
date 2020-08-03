using System;
using UnityEngine;

namespace UnityCommunityVoxelProject.General.Controls
{
    public class PlayerController : Singleton<PlayerController>
    {

        [HideInInspector] public Transform tf;

        private void Start()
        {
            tf = GetComponent<Transform>();
        }
    }
}