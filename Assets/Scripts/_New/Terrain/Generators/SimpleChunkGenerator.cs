using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;

namespace UnityVoxelCommunityProject.Terrain.ProceduralGeneration
{
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
    public struct SimpleChunkGenerator : IJobParallelFor
    {
        public NativeArray<Block> currentChunk;
            
        public float2 chunkPosition;
        public int    width, height;
        public int    seaLevel;
        
        private       int i, x, y, z;

        public void Execute(int index)
        {
            i = index;
                
            //(width * height * z) + (width * y) + x
            z = i / (width * height);
            i -= (width * height * z);
            y = (i / width);
            x = i % width;
            
            float snoiseResult = (math.unlerp(-1, 1, noise.snoise(new float2(x + (chunkPosition.x * width), z + (chunkPosition.y * width)) * 0.025f)) * 10);  
                
            float heightMap  = height * 0.5f + snoiseResult;
            float stoneLevel = (height * 0.25f) + snoiseResult / 2;
                
            currentChunk[index] = Block.Air;
            if (y <= stoneLevel)
            {
                currentChunk[index] = Block.Stone;
            }
            else if (y <= heightMap)
            {
                currentChunk[index] = Block.Dirt;

                if (y > heightMap - 1 && y > seaLevel - 2)
                {
                    currentChunk[index] = Block.Grass;
                }
            }
                
            if (y <= snoiseResult/5)
            {
                currentChunk[index] = Block.Core;
            }
        }
    }
}