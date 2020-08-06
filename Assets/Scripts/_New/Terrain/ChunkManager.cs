using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityCommunityVoxelProject.Serialization;
using UnityCommunityVoxelProject.Utility;
using UnityEngine;

namespace UnityCommunityVoxelProject.Terrain
{
    public class ChunkManager : Singleton<ChunkManager>
    {
        public GameObject chunkPrefab;

        public WorldData worldData;
        
        private List<Chunk> usedChunks = new List<Chunk>();
        private List<Chunk> chunksPool = new List<Chunk>();

        private Transform tf;
        [HideInInspector] public int blocksPerChunk;

        public void Initialize()
        {
            tf = GetComponent<Transform>();
            blocksPerChunk = SettingsHolder.Instance.proceduralGeneration.chunkWidth *
                             SettingsHolder.Instance.proceduralGeneration.chunkWidth *
                             SettingsHolder.Instance.proceduralGeneration.chunkHeight;


            Local();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Local();
            }
        }

        private void Local()
        {
            float time = Time.realtimeSinceStartup;
            int width  = SettingsHolder.Instance.proceduralGeneration.chunkWidth;

            int i = 0;
            
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                var generatedChunkData = 
                    TerrainProceduralGeneration.Instance.GenerateChunk(new ProtoInt2(x * width, y * width)); 
                
                worldData.chunks.Add(new ProtoInt2(x, y), generatedChunkData);
                i++;
            }

            Debug.Log($"Terrain data generation took around {Time.realtimeSinceStartup - time}s. Generated {i} chunks.");

            /*for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                SimpleCreate(new Vector3(x * width, 0, y * width));
            }*/
        }

        private void SimpleCreate(Vector3 position)
        {
            var created = Instantiate(chunkPrefab, tf).GetComponent<Chunk>();
            created.name = $"Chunk [x{position.x} z{position.z}]";
            created.transform.position = position;
            chunksPool.Add(created);

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