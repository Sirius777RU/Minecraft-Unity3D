using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityCommunityVoxelProject.Utility;
using UnityEngine;

namespace UnityCommunityVoxelProject.Terrain
{
    public class ChunkManager : Singleton<ChunkManager>
    {
        public bool useJobSystem = true;
        
        public GameObject chunkPrefab;

        private List<Chunk> usedChunks = new List<Chunk>();
        private List<Chunk> chunksPool = new List<Chunk>();

        private Transform tf;
        private int       blocksPerChunk;

        private void Start()
        {
            tf = GetComponent<Transform>();
            blocksPerChunk = SettingsHolder.Instance.proceduralGeneration.chunkWidth *
                             SettingsHolder.Instance.proceduralGeneration.chunkWidth *
                             SettingsHolder.Instance.proceduralGeneration.chunkHeight;


            Local();
        }
        private void Local()
        {
            float time = Time.realtimeSinceStartup;
            int width = SettingsHolder.Instance.proceduralGeneration.chunkWidth;
            
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                SimpleCreate(new Vector3(x * width, 0, y * width));
            }
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