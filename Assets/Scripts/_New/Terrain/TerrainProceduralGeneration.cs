using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace UnityCommunityVoxelProject.Terrain
{
    public class TerrainProceduralGeneration : Singleton<TerrainProceduralGeneration>
    {
        public void GenerateChunk(NativeArray<Block> blocks)
        {
            int width = 16;
            int height = 64;
            
            int areaSquare = width * width;
            int totalBlocksCount = (width * width) * height;

            float time = Time.realtimeSinceStartup;
            
            //Random randomDominant   = new Random(1337);
            //Random randomSupporting = new Random(1338);
            
            ChunkGenerationJob chunkGenerationJob = new ChunkGenerationJob()
            {
                width      = width,
                height     = height,
                areaSquare = areaSquare,
                    
                blocks = blocks
            };

            var handle = chunkGenerationJob.Schedule(totalBlocksCount, 64);
            handle.Complete();

            //Debug.Log(Time.realtimeSinceStartup - time);
        }
        
        
        [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        private struct ChunkGenerationJob : IJobParallelFor
        {
            public int width, height;
            public int areaSquare;
            public NativeArray<Block> blocks;
            
            public void Execute(int index)
            {
                int z =  index / areaSquare;
                int y = (index / width) % width;
                int x =  index % width;

                int seaLevel = 28;
                
                float heightMap = height * 0.5f + 
                                  math.unlerp(-1, 1, noise.snoise(new float2(x, y) * 0.025f));

                blocks[index] = Block.Air;
                if (y <= heightMap)
                {
                    blocks[index] = Block.Dirt;
                }
            }
        }
    }
}