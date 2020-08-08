using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using UnityVoxelCommunityProject.Serialization;
using UnityVoxelCommunityProject.Utility;
using Random = UnityEngine.Random;

namespace UnityVoxelCommunityProject.Terrain
{
    public class ChunkManager : Singleton<ChunkManager>
    {
        public GameObject chunkPrefab;

        public WorldData worldData;
        
        private List<Chunk> usedChunks = new List<Chunk>();
        private List<Chunk> chunksPool = new List<Chunk>();

        private Transform tf;
        [HideInInspector] public int blocksPerChunk;
        private CurrentGenerationSettings settings;
        private int width, height, widthSqr;

        public NativeArray<int2> atlasMap;
        
        public void Initialize()
        {
            tf = GetComponent<Transform>();
            SettingsHolder.Instance.blockData.GrabUVMappingArray(out atlasMap, Allocator.Persistent);
            settings = SettingsHolder.Instance.proceduralGeneration;

            width    = settings.chunkWidth;
            height   = settings.chunkHeight;
            widthSqr = width * width;
            
            blocksPerChunk = (width) * (width) * 
                             height;

            
            Local();
        }

        //Use global block position to get block type.
        public Block GetBlockAtPosition(int3 blockPosition)
        {
            int2 chunkPosition; 
            
            //Compatible with negative chunk positions.
            chunkPosition.x = Mathf.FloorToInt((0f + blockPosition.x) / width);
            chunkPosition.y = Mathf.FloorToInt((0f + blockPosition.z) / width);
            //chunkPosition.x = Mathf.FloorToInt(blockPosition.x / width);
            //chunkPosition.y = Mathf.FloorToInt( blockPosition.z / width);
            
            blockPosition.x = blockPosition.x >= 0 ? ((blockPosition.x % width + width) % width) : ((blockPosition.x % width + width) % width);
            blockPosition.z = blockPosition.z >= 0 ? ((blockPosition.z % width + width) % width) : ((blockPosition.z % width + width) % width);
            //blockPosition.x = blockPosition.x % width;
            //blockPosition.z = blockPosition.z % width;

            return GetBlockAtPosition(chunkPosition, blockPosition);
        }
        
        //Use chunk and local block position to get block type.
        public Block GetBlockAtPosition(int2 chunkPosition, int3 blockPosition)
        {
            int i = blockPosition.x + blockPosition.z * width + blockPosition.y * widthSqr;
            if (i < 0)
            {
                return Block.Core;
            }
            return worldData.chunks[chunkPosition].blocks[i];
        }

        //Get blocks in certain volume.
        //TODO just provide required chunks to mesh generation instead of this.
        public void GetBlocksVolume(int3 from, int3 to, NativeArray<Block> blocks)
        {
            int i = 0;
            
            //Debug.Log($"Volume from {from} to {to}");
            for (int y = from.y; y < to.y; y++)
            for (int z = from.z; z < to.z; z++)
            for (int x = from.x; x < to.x; x++)
            {
                blocks[i] = Random.Range(0, 20) < 2 ? Block.Core : Block.Air; 
                //blocks[i] = GetBlockAtPosition(new int3(x, y, z));
                i++;
            }
        }

        private void Update()
        {
            /*if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Local();
            }*/
            
            if(Input.GetKeyDown(KeyCode.Alpha5))
            {
                float time = Time.realtimeSinceStartup;
                
                for (int i = 0; i < usedChunks.Count; i++)
                {
                    usedChunks[i].Local();
                }

                Debug.Log($"Chunks rebuild took: {Time.realtimeSinceStartup - time}s");
            }
        }

        public void Local()
        {
            float time = Time.realtimeSinceStartup;
            int renderDistance = SettingsHolder.Instance.displayOptions.chunkRenderDistance;
            
            for (int z = 0; z < renderDistance; z++)
            for (int x = 0; x < renderDistance; x++)
            {
                SimpleCreate(new Vector3(x * width, 0, z * width));
            }
            
            Debug.Log($"Full procedural terrain data and mesh generation took around {Time.realtimeSinceStartup - time}s. Generated in memory: {worldData.chunks.Count} chunks. Displaying right now: {usedChunks.Count} chunks. ");
            
        }

        private void SimpleCreate(Vector3 position)
        {
            var created = Instantiate(chunkPrefab, tf).GetComponent<Chunk>();
            created.name = $"Chunk [x{position.x} z{position.z}]";
            created.transform.position = position;
            usedChunks.Add(created);

            created.Initialize(blocksPerChunk);
            created.Local();
        }
        
        private void OnApplicationQuit()
        {
            for (int i = 0; i < usedChunks.Count; i++)
                usedChunks[i].Dispose();

            for (int i = 0; i < chunksPool.Count; i++)
                chunksPool[i].Dispose();

            atlasMap.Dispose();
        }
    }
}