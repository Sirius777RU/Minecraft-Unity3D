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
        public int    seaLevel;

        private       int i, x, y, z;

        public void Execute(int index)
        {
            i = index;

            y = i / (height);
            i = i % (height);
            z = i / width;
            x = i % width;
            
            float noiseScale = 0.02f;
            float3 noiseMapping = new float3(x + (chunkPosition.x * width),
                                             z + (chunkPosition.y * width),
                                            y * 2f);

            float noiseResult = math.pow(math.unlerp(-1, 1,noise.snoise(noiseMapping * noiseScale)) * 30, 0.9f);

            float heightMap  = (height * 0.3f) + noiseResult;
            float stoneLevel = (height * 0.25f) + noiseResult;

            bool underground = false;
            
            if (y <= stoneLevel)
            {
                currentChunk[index] = Block.Stone;
                underground = true;
                
                if (y == 0)
                    currentChunk[index] = Terrain.Block.Core;
            }
            else if (y <= heightMap)
            {
                currentChunk[index] = Block.Dirt;
                underground         = true;
            }

            if (underground && y > 0)
            {
                float3 caveMapping = new float3(noiseMapping.x,
                                                noiseMapping.y,
                                                y * 1.2f);
            
            
                float caveMask = math.unlerp(-1, 1,noise.snoise(caveMapping * 0.05f) * 10f);
                caveMask = (caveMask + 16) / 4;
            
                float caveNoise = math.unlerp(-1, 1,noise.snoise(caveMapping * 0.08f) * 10f);
                caveNoise = (caveNoise + 6f) / 5;
                if (caveNoise < 1.45f && caveMask < 3.6f)
                {
                    currentChunk[index] = Block.Air;
                }
                else
                {
                    //currentChunk[index] = Block.Stone;
                }
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