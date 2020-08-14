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
        
        public Dictionary<int2, Tuple<JobHandle, DataChunk>> currentlyInGenerationMap = new Dictionary<int2, Tuple<JobHandle, DataChunk>>();

        private void OnApplicationQuit()
        {
            CompleteAll();
        }

        public void RequestChunkGeneration(int2 chunkPosition, bool withNeighbors)
        {
            if (!withNeighbors)
            {
                if (currentlyInGenerationMap.ContainsKey(chunkPosition))
                {
                    var tuple = currentlyInGenerationMap[chunkPosition];
                    tuple.Item1.Complete();
                    tuple.Item2.ready = true;
                    
                    currentlyInGenerationMap.Remove(chunkPosition);
                }
                else
                {
                    currentlyInGenerationMap.Add(chunkPosition, PrepareChunkGeneration(chunkPosition));
                }
            }
            else
            {
                chunkPosition -= new int2(1, 1);
                for (int z = 0; z < 3; z++)
                for (int x = 0; x < 3; x++)
                {
                    var position = chunkPosition + new int2(x, z); 
                    
                    if (currentlyInGenerationMap.ContainsKey(position))
                    {
                        var tuple = currentlyInGenerationMap[position];
                        tuple.Item1.Complete();
                        tuple.Item2.ready = true;
                        
                        currentlyInGenerationMap.Remove(position);
                        continue;
                    }
                    
                    if(ChunkManager.Instance.dataWorld.chunks.ContainsKey(position))
                        continue;
                    
                    currentlyInGenerationMap.Add(position, PrepareChunkGeneration(position));
                }
            }
            
            
        }
        
        private Tuple<JobHandle, DataChunk> PrepareChunkGeneration(int2 chunkPosition)
        {
            DataChunk dataChunk = new DataChunk()
            {
                blocks = new NativeArray<Block>(ChunkManager.Instance.blocksPerChunk, Allocator.Persistent)
            };

            ChunkManager.Instance.dataWorld.chunks.Add(chunkPosition, dataChunk);
            
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

                    currentChunk = dataChunk.blocks
                };
                return new Tuple<JobHandle, DataChunk>(simpleChunkGenerator.Schedule(totalBlocksCount - 1, batchParallelFor), dataChunk);
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

                    currentChunk = dataChunk.blocks
                };
                
                RegularChunkPostProcessJob regularChunkPostProcessJob = new RegularChunkPostProcessJob()
                {
                    chunkPosition = chunkPosition,
                    
                    width      = width,
                    height     = height,
                    areaSquare = areaSquare,
                    seaLevel   = seaLevel,

                    currentChunk = dataChunk.blocks
                };
                
                var handle = regularGenerationJob.Schedule(totalBlocksCount - 1, batchParallelFor);
                return new Tuple<JobHandle, DataChunk>(regularChunkPostProcessJob.Schedule(handle), dataChunk);
            }
            #endregion
        }

        public void Complete(int2 position, bool withNeighbors = false)
        {
            CompleteAndRemoveJob(position);

            if (withNeighbors)
            {
                CompleteAndRemoveJob(position + new int2(1, 0));
                CompleteAndRemoveJob(position + new int2(-1, 0));
                CompleteAndRemoveJob(position + new int2(0, 1));
                CompleteAndRemoveJob(position + new int2(0, -1));
                
                CompleteAndRemoveJob(position + new int2(1, 1));
                CompleteAndRemoveJob(position + new int2(1, -1));
                CompleteAndRemoveJob(position + new int2(-1, -1));
                CompleteAndRemoveJob(position + new int2(-1, 1));
            }

            void CompleteAndRemoveJob(int2 localPosition)
            {
                if (currentlyInGenerationMap.ContainsKey(localPosition))
                {
                    var tuple = currentlyInGenerationMap[localPosition]; 
                    tuple.Item1.Complete();
                    tuple.Item2.ready = true;
                    
                    currentlyInGenerationMap.Remove(localPosition);
                }
            }
        }

        public void CompleteAll()
        {
            foreach (var keyValuePair in currentlyInGenerationMap)
            {
                var tuple = keyValuePair.Value;
                tuple.Item1.Complete();
                tuple.Item2.ready = true;
            }
            
            currentlyInGenerationMap.Clear();
        }
    }
}