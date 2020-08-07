using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityVoxelCommunityProject.Terrain;

namespace UnityVoxelCommunityProject.General
{
    [CreateAssetMenu(fileName = "Blocks Data", menuName = "Settings/Blocks Data", order = 0)]
    public class BlocksDataSettings : ScriptableObject
    {
        public BlockData[] blocks = new BlockData[]
        {
            new BlockData()
            {
                name = "Dirt",
                atlasMapping = new BlockUV()
                {
                    defaultTexture    = new int2(0, 0),
                    sideTexture   = new int2(-1, -1),
                    bottomTexture = new int2(-1, -1)
                },
                blockType = Block.Dirt,
                
                durability = 10
            }, 
        };

        public void GrabUVMappingArray(out NativeArray<int2> data, Allocator allocator)
        {
            data = new NativeArray<int2>(blocks.Length * 3, allocator);
            
            for (int i = 0; i < blocks.Length; i++)
            {
                data[(i * 3)]     = blocks[i].atlasMapping.Default();
                data[(i * 3) + 1] = blocks[i].atlasMapping.Side();
                data[(i * 3) + 2] = blocks[i].atlasMapping.Bottom();
            }
        }

        public Dictionary<Block, BlockUV> GrabUVMappingDictionary()
        {
            var result = new Dictionary<Block, BlockUV>();

            for (int i = 0; i < blocks.Length; i++)
            {
                result.Add(blocks[i].blockType, blocks[i].atlasMapping);
            }
            
            return result;
        }
    }
}