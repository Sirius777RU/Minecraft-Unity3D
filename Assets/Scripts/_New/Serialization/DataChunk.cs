using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityVoxelCommunityProject.Terrain;

namespace UnityVoxelCommunityProject
{
    //TODO add isDirty flag and serialize only changed chunks instead of rewriting entire game file.
    public class DataChunk
    {
        public NativeArray<Block> blocks;

        public bool isReady;
    }
}