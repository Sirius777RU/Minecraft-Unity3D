using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;

namespace UnityVoxelCommunityProject.Terrain.ProceduralGeneration
{
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
    public struct RegularChunkGenerationJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<Terrain.Block> currentChunk;

        public float2 chunkPosition;
        public int    width, height;
        public int    areaSquare;
        public int    seaLevel;

        private       int i, x, y, z;

        public void Execute(int index)
        {
            i = index;

            y = i / (areaSquare);
            i = i % (areaSquare);
            z = i / width;
            x = i % width;
            
            float3 noiseMapping = new float3(x + (chunkPosition.x * width),
                                             z + (chunkPosition.y * width),
                                            y * 2f)
                                * 0.02f;
            
            float noiseResult = math.pow(math.unlerp(-1, 1,noise.snoise(noiseMapping)) * 30, 0.9f);


            float heightMap  = height * 0.3f + noiseResult;
            float stoneLevel = (height * 0.25f) + noiseResult / 2;

            /*float3 noiseMapping = new float3(x + (chunkPosition.x * width), z + (chunkPosition.y * width), y) * 0.015f;

            float noiseResult = math.unlerp(-1, 1, noise.snoise(noiseMapping));
            if (noiseResult > 0.4f)
            {
                currentChunk[index] = Terrain.Block.Dirt;
            }
            else
            {
                currentChunk[index] = Terrain.Block.Air;
            }*/


            if (y <= stoneLevel)
            {
                currentChunk[index] = Block.Stone;
            }
            else if (y <= heightMap)
            {
                currentChunk[index] = Block.Dirt;

                /*if (y > heightMap - 1 && y > seaLevel - 2)
                {
                    currentChunk[index] = Block.Grass;
                }*/
            }

            if (y == 0)
            {
                currentChunk[index] = Terrain.Block.Core;
            }
        }
    }

    [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
    public struct RegularChunkPostProcessJob : IJob
    {
        public NativeArray<Terrain.Block> currentChunk;

        public float2 chunkPosition;
        public int    width, height;
        public int    areaSquare;
        public int    seaLevel;
        
        public void Execute()
        {
            //Look down to see if there is dirt.
            for (int x = 0; x < width; x++)
            for (int z = 0; z < width; z++)
            for (int y = height-1; y > seaLevel; y--)
            {
                int index = x + z * width + y * areaSquare;
                Block currentBlock = currentChunk[index];
                if (currentBlock != Block.Air)
                {
                    if (currentBlock == Block.Dirt)
                    {
                        currentChunk[index] = Block.Grass;
                    }
                    break;
                }
            }
            
        }
    }
}