using System;
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
        public                  bool useJobSystem     = true;
        [Range(1, 4096)] public int  batchParallelFor = 64;

        public void RequestChunkGeneration(int2 chunkPosition, bool withNeighbors)
        {
            if (!withNeighbors)
            {
                GenerateChunk(chunkPosition);
            }
            else
            {
                chunkPosition -= new int2(1, 1);

                for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                {
                    GenerateChunk(chunkPosition + new int2(x, y));
                }
            }
        }
        
        private void GenerateChunk(int2 chunkPosition)
        {
            if (ChunkManager.Instance.worldData.chunks.ContainsKey(chunkPosition))
            {
                return;
            }

            ChunkData chunkData = new ChunkData()
            {
                blocks = new NativeArray<Block>(ChunkManager.Instance.blocksPerChunk, Allocator.Persistent)
            };

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

                if (useJobSystem)
                {
                    simpleChunkGenerator.Schedule(totalBlocksCount - 1, batchParallelFor).Complete();
                }
                else
                {
                    for (int i = 0; i < totalBlocksCount - 1; i++)
                    {
                        simpleChunkGenerator.Execute(i);
                    }
                }

            }
            else if (currentGenerator == Generator.Regular)
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

                if (useJobSystem)
                {
                    var handle = regularGenerationJob.Schedule(totalBlocksCount - 1, batchParallelFor);
                    handle = regularChunkPostProcessJob.Schedule(handle);
                    handle.Complete();
                }
                else
                {
                    for (int i = 0; i < totalBlocksCount - 1; i++)
                    {
                        regularGenerationJob.Execute(i);
                    }
                    
                    regularChunkPostProcessJob.Execute();
                }
            }
            #endregion
            
            ChunkManager.Instance.worldData.chunks.Add(chunkPosition, chunkData);
            
        }

        
        
        
    }
}