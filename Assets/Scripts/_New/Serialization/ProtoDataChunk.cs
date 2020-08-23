using System;
using System.Collections.Generic;
using ProtoBuf;
using Unity.Collections;
using Unity.Mathematics;
using UnityVoxelCommunityProject.Terrain;

namespace UnityVoxelCommunityProject
{
    [ProtoContract]
    public class ProtoDataChunk
    {
        [ProtoMember(1)] public Block[] blocks;
        //TODO [ProtoMember(2)] public List lightSources

        public static implicit operator DataChunk(ProtoDataChunk p)
        {
            return new DataChunk()
            {
                blocks = new NativeArray<Block>(p.blocks, Allocator.Persistent),
                lightSources = new List<Tuple<int3, byte>>(),
                isReady = true
            };
        }

        public static implicit operator ProtoDataChunk(DataChunk p)
        {
            var result = new ProtoDataChunk()
            {
                blocks = new Block[p.blocks.Length]
            };
            p.blocks.CopyTo(result.blocks);

            return result;
        }
    }
}