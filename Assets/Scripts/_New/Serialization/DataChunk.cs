using Unity.Collections;
using UnityVoxelCommunityProject.Terrain;

namespace UnityVoxelCommunityProject
{
    public class DataChunk
    {
        public bool               ready;
        public NativeArray<Block> blocks;
    }
}