using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityVoxelCommunityProject.Terrain
{
    [Serializable]
    public class BlockData
    {
        public string  name;
        public Block   blockType;
        public BlockUV atlasMapping;

        public float durability;

        public BlockData()
        {
            name      = "New Block";
            blockType = Block.Core;

            atlasMapping = new BlockUV()
            {
                defaultTexture = new int2(0, 0),
                sideTexture    = new int2(-1, -1),
                bottomTexture  = new int2(-1, -1)
            };
            
            
            durability = Single.PositiveInfinity;
        }
    }
}