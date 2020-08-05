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
            var created = Instantiate(chunkPrefab, tf).GetComponent<Chunk>();
            chunksPool.Add(created);

            created.Initialize(blocksPerChunk);
            created.Local();
            
            created = Instantiate(chunkPrefab, tf).GetComponent<Chunk>();
            created.transform.position = new Vector3(16, 0, 0);
            chunksPool.Add(created);

            created.Initialize(blocksPerChunk);
            created.Local();
            
            created = Instantiate(chunkPrefab, tf).GetComponent<Chunk>();
            created.transform.position = new Vector3(16, 0, 16);
            chunksPool.Add(created);

            created.Initialize(blocksPerChunk);
            created.Local();
            
            created = Instantiate(chunkPrefab, tf).GetComponent<Chunk>();
            created.transform.position = new Vector3(0, 0, 16);
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