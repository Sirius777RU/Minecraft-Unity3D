using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using UnityVoxelCommunityProject.Serialization;
using UnityVoxelCommunityProject.Utility;

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

        public int value = -1;
        
        public void Initialize()
        {
            tf = GetComponent<Transform>();
            settings = SettingsHolder.Instance.proceduralGeneration;
            
            width    = settings.chunkWidth;
            height   = settings.chunkHeight;
            widthSqr = width * width;
            
            //Block count here gets extra 1 blocks for each chunk side so we could check neighboring to current chunk blocks.
            blocksPerChunk = (width + 2) * (width + 2) * 
                             height;
            
            Local();
        }

        private void Update()
        {
            if (Input.GetKeyDown(key: KeyCode.Alpha3))
            {
                int result = value >= 0 ? ((value % width + width) % width) : ((value % width + width) % width) + 1;
                Debug.Log(message: $"Result: {result}"); 
            }
            
            if (Input.GetKeyDown(key: KeyCode.Alpha2))
            {
                Local();
            }
        }

        //Use global block position to get block type.
        public Block GetBlockAtPosition(int3 blockPosition)
        {
            int2 chunkPosition; 
            //Debug.Log($"BlockPosition {blockPosition}");
            
            //Compatible with negative chunk positions.
            chunkPosition.x = Mathf.FloorToInt((0f + blockPosition.x) / width);
            chunkPosition.y = Mathf.FloorToInt((0f + blockPosition.z) / width);
            //chunkPosition.x = Mathf.FloorToInt(blockPosition.x / width);
            //chunkPosition.y = Mathf.FloorToInt( blockPosition.z / width);
            
            blockPosition.x = blockPosition.x >= 0 ? ((blockPosition.x % width + width) % width) : ((blockPosition.x % width + width) % width);
            blockPosition.z = blockPosition.z >= 0 ? ((blockPosition.z % width + width) % width) : ((blockPosition.z % width + width) % width);
            //blockPosition.x = blockPosition.x % width;
            //blockPosition.z = blockPosition.z % width;
            
            //Debug.Log($"Converted to chunk {chunkPosition} Block {blockPosition}");

            return GetBlockAtPosition(chunkPosition, blockPosition);
        }
        
        //Use chunk and local block position to get block type.
        public Block GetBlockAtPosition(int2 chunkPosition, int3 blockPosition)
        {
            int i = blockPosition.x + blockPosition.z * width + blockPosition.y * widthSqr;
            //Debug.Log($"index {i} {blockPosition}");
            if (i < 0)
            {
                return Block.Core;
            }
            
            //Debug.Log($"{i} {worldData.chunks[chunkPosition].blocks.Length}");
            return worldData.chunks[chunkPosition].blocks[i];
        }

        //Get blocks in certain volume.
        public void GetBlocksVolume(int3 from, int3 to, NativeArray<Block> blocks)
        {
            int i = 0;
            
            //Debug.Log($"Volume from {from} to {to}");
            Debug.Log($"{from} - {to}");
            for (int y = from.y; y < to.y; y++)
            for (int z = from.z; z < to.z; z++)
            for (int x = from.x; x < to.x; x++)
            {
                //i = x + z * (width+2) + y * (widthSqr + 4);
                if (i >= blocks.Length)
                {
                    Debug.Log($"{x} {z} {y}");
                }
                blocks[i] = GetBlockAtPosition(new int3(x, y, z));

                if (x == 8 && z == 17 && y == 36)
                {
                    Debug.Log($"Volume: At x{x} z{z} y{y} is {blocks[i]}. Index {i}");
                }
                i++;
            }

            //Debug.Log(blocks[0]);
        }

        private void Local()
        {
            float time = Time.realtimeSinceStartup;

            //TerrainProceduralGeneration.Instance.GenerateChunk(new int2(0, 0), true);

            /*Debug.Log(GetBlockAtPosition(new int3(8, 38, 14)));
            Debug.Log(GetBlockAtPosition(new int3(8, 38, 15)));
            Debug.Log(GetBlockAtPosition(new int3(8, 38, 16)));
            Debug.Log(GetBlockAtPosition(new int3(8, 38, 17)));
            Debug.Log(GetBlockAtPosition(new int3(8, 38, 18)));
            Debug.Log(GetBlockAtPosition(new int3(8, 38, 19)));*/
            
            
            int i = 0;

            for (int z = -2; z < 2; z++)
            for (int x = -2; x < 2; x++)
            {
                SimpleCreate(new Vector3(x * width, 0, z * width));
            }
            
            //Debug.Log(GetBlockAtPosition(new int3(0, 45, 0)));
            Debug.Log($"Terrain data generation took around {Time.realtimeSinceStartup - time}s. Generated {i} chunks.");
            
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
        }
    }
}