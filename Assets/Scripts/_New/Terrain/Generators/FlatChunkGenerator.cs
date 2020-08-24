using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;

namespace UnityVoxelCommunityProject.Terrain.ProceduralGeneration
{
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
    public struct FlatChunkGenerator : IJobParallelFor
    {
        public NativeArray<Block> currentChunk;
        
        public int    width, height;
        
        private       int i, x, y, z;

        public void Execute(int index)
        {
            i = index;
                
            z = i / (width * height);
            i -= (width * height * z);
            y = (i / width);
            //x = i % width;

            if (y < 73)
            {
                if (y == 72)
                {
                    currentChunk[index] = Block.Grass;
                }
                else
                {
                    currentChunk[index] = Block.Dirt;
                }
            }
        }
    }
}