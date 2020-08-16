using ProtoBuf;
using Unity.Collections;
using UnityVoxelCommunityProject.Terrain;

namespace UnityVoxelCommunityProject
{
    [ProtoContract]
    public class ProtoDataChunk
    {
        [ProtoMember(1)] public Block[] blocks;

        public static implicit operator DataChunk(ProtoDataChunk p)
        {
            return new DataChunk()
            {
                blocks = new NativeArray<Block>(p.blocks, Allocator.Persistent),
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