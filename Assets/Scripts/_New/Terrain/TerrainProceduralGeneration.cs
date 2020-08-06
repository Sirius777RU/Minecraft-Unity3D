using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityCommunityVoxelProject.Serialization;
using UnityCommunityVoxelProject.Utility;
using UnityEngine;

using Random = Unity.Mathematics.Random;

namespace UnityCommunityVoxelProject.Terrain
{
    public class TerrainProceduralGeneration : Singleton<TerrainProceduralGeneration>
    {
        public bool useJobSystem = true;
        [Range(1, 4096)] public int batchParallelFor = 64;
        
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

            if (useJobSystem)
            {
                var handle = chunkGenerationJob.Schedule(totalBlocksCount-1, batchParallelFor);
                handle.Complete();
            }
            else
            {
                for (int i = 0; i < totalBlocksCount-1; i++)
                {
                    chunkGenerationJob.Execute(i);
                }
            }

            //Debug.Log($"Generation took: {Time.realtimeSinceStartup - time}");
        }

        public ChunkData GenerateChunk(int2 chunkPosition)
        {
            ChunkData chunkData = new ChunkData()
            {
                blocks = new Block[ChunkManager.Instance.blocksPerChunk]
            };
            
            int width  = SettingsHolder.Instance.proceduralGeneration.chunkWidth;
            int height = SettingsHolder.Instance.proceduralGeneration.chunkHeight;
            
            int areaSquare       = width * width;
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
                    
                blocks = new NativeArray<Block>(chunkData.blocks, Allocator.TempJob)
            };

            if (useJobSystem)
            {
                var handle = chunkGenerationJob.Schedule(totalBlocksCount-1, batchParallelFor);
                handle.Complete();
            }
            else
            {
                for (int i = 0; i < totalBlocksCount-1; i++)
                {
                    chunkGenerationJob.Execute(i);
                }
            }
            

            chunkGenerationJob.blocks.CopyTo(chunkData.blocks);
            chunkGenerationJob.Dispose();
            
            return chunkData;
        }
        
        
        [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        private struct ChunkGenerationJob : IJobParallelFor
        {
            public NativeArray<Block> blocks;
            
            public float2 chunkPosition;
            public int width, height;
            public int areaSquare;
            
            private int i, x, y, z;
            private const int seaLevel = 28;

            public void Execute(int index)
            {
                i = index;
                
                y = i / (areaSquare);
                i = i % (areaSquare);
                z = i / width;
                x = i % width;
                
                float heightMap = height * 0.5f + 
                                 (math.unlerp(-1, 1, noise.snoise(new float2(x + chunkPosition.x, z + chunkPosition.y) * 0.025f)) * 10);

                blocks[index] = Block.Air;
                if (y <= heightMap)
                {
                    blocks[index] = Block.Dirt;
                }
            }

            public void Dispose()
            {
                blocks.Dispose();
            }
        }
    }

}