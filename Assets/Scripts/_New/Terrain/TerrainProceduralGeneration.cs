using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityVoxelCommunityProject.Serialization;
using UnityVoxelCommunityProject.Terrain.ProceduralGeneration;
using UnityVoxelCommunityProject.Utility;
using Random = Unity.Mathematics.Random;

namespace UnityVoxelCommunityProject.Terrain
{
    public class TerrainProceduralGeneration : Singleton<TerrainProceduralGeneration>
    {
        public Generator currentGenerator = Generator.Regular;
        [Space(10)]
        [Range(1, 4096)] public int  batchParallelFor = 64;
        
        public Dictionary<int2, JobHandle> currentlyInGenerationMap = new Dictionary<int2, JobHandle>();

        public void RequestChunkGeneration(int2 chunkPosition, bool withNeighbors, bool instant = true)
        {
            if (!withNeighbors)
            {
                JobHandle handle = PrepareChunkGeneration(chunkPosition);
                currentlyInGenerationMap.Add(chunkPosition, handle);
            }
            else
            {
                chunkPosition -= new int2(1, 1);
                for (int z = 0; z < 3; z++)
                for (int x = 0; x < 3; x++)
                {
                    if(ChunkManager.Instance.worldData.chunks.ContainsKey(chunkPosition + new int2(x, z)))
                        continue;
                    
                    currentlyInGenerationMap.Add(chunkPosition + new int2(x, z),
                                                 PrepareChunkGeneration(chunkPosition + new int2(x, z)));
                }
            }
            
            
        }
        
        private JobHandle PrepareChunkGeneration(int2 chunkPosition)
        {
            ChunkData chunkData = new ChunkData()
            {
                blocks = new NativeArray<Block>(ChunkManager.Instance.blocksPerChunk, Allocator.Persistent)
            };

            ChunkManager.Instance.worldData.chunks.Add(chunkPosition, chunkData);
            
            int width  = SettingsHolder.Instance.proceduralGeneration.chunkWidth;
            int height = SettingsHolder.Instance.proceduralGeneration.chunkHeight;
            int seaLevel = SettingsHolder.Instance.proceduralGeneration.seaLevel;
            
            int areaSquare       = width * width;
            int totalBlocksCount = (width * width) * height;

            float time = Time.realtimeSinceStartup;

            #region Generators
            if (currentGenerator == Generator.Simple)
            {
                SimpleChunkGenerator simpleChunkGenerator = new SimpleChunkGenerator()
                {
                    chunkPosition = chunkPosition,

                    width      = width,
                    height     = height,
                    areaSquare = areaSquare,
                    seaLevel   = seaLevel,

                    currentChunk = chunkData.blocks
                };

                return simpleChunkGenerator.Schedule(totalBlocksCount - 1, batchParallelFor);
            }
            else //if (currentGenerator == Generator.Regular)
            {
                RegularChunkGenerationJob regularGenerationJob = new RegularChunkGenerationJob()
                {
                    chunkPosition = chunkPosition,

                    width      = width,
                    height     = height,
                    areaSquare = areaSquare,
                    seaLevel   = seaLevel,

                    currentChunk = chunkData.blocks
                };
                
                RegularChunkPostProcessJob regularChunkPostProcessJob = new RegularChunkPostProcessJob()
                {
                    chunkPosition = chunkPosition,
                    
                    width      = width,
                    height     = height,
                    areaSquare = areaSquare,
                    seaLevel   = seaLevel,

                    currentChunk = chunkData.blocks
                };
                
                var handle = regularGenerationJob.Schedule(totalBlocksCount - 1, batchParallelFor);
                return regularChunkPostProcessJob.Schedule(handle);
            }
            #endregion
        }

        public void Complete(int2 position)
        {
            if (currentlyInGenerationMap.ContainsKey(position))
            {
                currentlyInGenerationMap[position].Complete();
            }
        }
        
        
    }
}