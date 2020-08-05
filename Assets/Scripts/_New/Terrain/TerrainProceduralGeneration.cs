using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityCommunityVoxelProject.Utility;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace UnityCommunityVoxelProject.Terrain
{
    public class TerrainProceduralGeneration : Singleton<TerrainProceduralGeneration>
    {
        public void GenerateChunk(NativeArray<Block> blocks, int2 chunkPosition)
        {
            int width = SettingsHolder.Instance.proceduralGeneration.chunkWidth;
            int height = SettingsHolder.Instance.proceduralGeneration.chunkHeight;
            
            int areaSquare = width * width;
            int totalBlocksCount = (width * width) * height;

            float time = Time.realtimeSinceStartup;
            
            //Random randomDominant   = new Random(1337);
            //Random randomSupporting = new Random(1338);
            
            ChunkGenerationJob chunkGenerationJob = new ChunkGenerationJob()
            {
                chunkPosition = chunkPosition,
                
                width      = width,
                height     = height,
                areaSquare = areaSquare,
                    
                blocks = blocks
            };

            var handle = chunkGenerationJob.Schedule(totalBlocksCount, 64);
            handle.Complete();

            //Debug.Log($"Generation took: {Time.realtimeSinceStartup - time}");
        }

        public float generationPlace;
        
        [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        private struct ChunkGenerationJob : IJobParallelFor
        {
            public float2 chunkPosition;
            public int width, height;
            public int areaSquare;
            public NativeArray<Block> blocks;
            
            public void Execute(int index)
            {
                int i = index;
                
                int y = i / (width * width);
                i = i % (width * width);
                int z = i / width;
                int x = i % width;

                int seaLevel = 28;
                
                float heightMap = height * 0.5f + 
                                  (math.unlerp(-1, 1, noise.snoise(new float2(x + chunkPosition.x, z + chunkPosition.y) * 0.025f)) * 10);

                blocks[index] = Block.Air;
                if (y <= heightMap)
                {
                    blocks[index] = Block.Dirt;
                }
            }
        }
    }
}