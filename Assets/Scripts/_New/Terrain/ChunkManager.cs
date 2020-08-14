using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using UnityVoxelCommunityProject.General;
using UnityVoxelCommunityProject.General.Controls;
using UnityVoxelCommunityProject.Serialization;
using UnityVoxelCommunityProject.Utility;

namespace UnityVoxelCommunityProject.Terrain
{
    public class ChunkManager : Singleton<ChunkManager>
    {
        public int framesPerStage = 3;
        public int maximumStageChangesPerFrame = 1;
        public int initialOffset = 128;
        [Space(10)]
        public bool updateColliders = true;
        public int instantDistance;
        public GameObject chunkPrefab;

        public WorldData worldData;
        
        private List<Chunk> usedChunks = new List<Chunk>();
        private Queue<Chunk> chunksPool = new Queue<Chunk>();
        private Queue<Chunk> chunksProcessing = new Queue<Chunk>();

        private Dictionary<int2, Chunk> usedChunksMap = new Dictionary<int2, Chunk>();

        [HideInInspector] public int blocksPerChunk;
        private Transform tf, playerTf;
        
        private CurrentGenerationSettings settings;
        private int width, height, widthSqr;
        private int2 lastPlayerPosition = new int2(Double.NegativeInfinity);

        public NativeArray<int2> atlasMap;
        
        public void Initialize()
        {
            tf = GetComponent<Transform>();
            playerTf = PlayerMovement.Instance.tf;
            SettingsHolder.Instance.blockData.GrabUVMappingArray(out atlasMap, Allocator.Persistent);
            settings = SettingsHolder.Instance.proceduralGeneration;

            width    = settings.chunkWidth;
            height   = settings.chunkHeight;
            widthSqr = width * width;
            
            blocksPerChunk = (width) * (width) * 
                             height;

            FillPool();
            Local();
            
            PlayerMovement.Instance.Landing();
        }

        private void Update()
        {
            Local();

            if(Input.GetKeyDown(KeyCode.Alpha5))
            {
                UpdateChunks();
            }

            int length = chunksProcessing.Count;
            for (int i = 0; i < length; i++)
            {
                
                var chunk = chunksProcessing.Dequeue();
                chunk.Local(false);

                if(chunk.currentStage == ChunkProcessing.Finished)
                    continue;

                if (i < maximumStageChangesPerFrame &&
                    chunk.framesInCurrentProcessingStage >= framesPerStage)
                {
                    chunk.readyForNextStage = true;
                }
                
                chunksProcessing.Enqueue(chunk);
            }
        }

        public void UpdateChunks()
        {
            float time = Time.realtimeSinceStartup;
                
            for (int i = 0; i < usedChunks.Count; i++)
            {
                usedChunks[i].Local(true);
            }

            Debug.Log($"Chunks rebuild took: {Time.realtimeSinceStartup - time}s");
        }

        private IEnumerator WaitTillNextFrameToDoLocal()
        {
            yield return WaitFor.Frames(1);

            Local();
        }

        public void Local()
        {
            int2 playerChunkPosition = PlayerMovement.Instance.playerChunkPosition;

            if (playerChunkPosition.x == lastPlayerPosition.x &&
                playerChunkPosition.y == lastPlayerPosition.y)
            {
                return;
            }
            
            int distance = SettingsHolder.Instance.displayOptions.chunkRenderDistance;
            ValidateChunks(distance);
            
            DisplayChunks(instantDistance, true);
            DisplayChunks(distance: distance, 
                           instant: false);
        }

        private void ValidateChunks(int renderDistance)
        {
            int2 displayPosition = PlayerMovement.Instance.playerChunkPosition + new int2(initialOffset, initialOffset);
            Queue<int2> chunksToFree = new Queue<int2>();
            
            var chunksKeys = usedChunksMap.Keys.ToArray();
            int length = chunksKeys.Length;
            for (int i = 0; i < length; i++)
            {
                int d = Mathf.Max(Mathf.Abs(chunksKeys[i].x - displayPosition.x), 
                                  Mathf.Abs(chunksKeys[i].y - displayPosition.y));
                
                if(d > renderDistance)
                    chunksToFree.Enqueue(chunksKeys[i]);
            }

            length = chunksToFree.Count;
            for (int i = 0; i < length; i++)
            {
                var chunkKey = chunksToFree.Dequeue();
                var chunk = usedChunksMap[chunkKey];
                usedChunksMap.Remove(chunkKey);
                
                chunk.FreeThisChunk();
                chunksPool.Enqueue(chunk);
            }
        }
        
        private List<int2> chunksToDisplay = new List<int2>();
        private void DisplayChunks(int distance, bool instant)
        {
            lastPlayerPosition = PlayerMovement.Instance.playerChunkPosition;
            chunksToDisplay.Clear();
            int2 finalOffset = new int2(initialOffset, initialOffset);
            
            if (distance <= 0)
            {
                chunksToDisplay.Add(new int2(0, 0));
            }
            else
            {
                for (int z = -(distance); z < (distance + 1); z++)
                for (int x = -(distance); x < (distance + 1); x++)
                {
                    chunksToDisplay.Add(new int2(x, z));
                }
            }

            chunksToDisplay.Sort((a, b) =>
            {
                return ((Mathf.Abs(a.x - lastPlayerPosition.x) + Mathf.Abs(a.y - lastPlayerPosition.y)) -
                        (Mathf.Abs(b.x - lastPlayerPosition.x) + Mathf.Abs(b.y - lastPlayerPosition.y)));
            });

            int length = chunksToDisplay.Count;
            for (int i = 0; i < length; i++)
            {
                int2 displayPosition = PlayerMovement.Instance.playerChunkPosition;
                displayPosition.x += chunksToDisplay[i].x;
                displayPosition.y += chunksToDisplay[i].y;

                if(usedChunksMap.ContainsKey(displayPosition + finalOffset))
                    continue;
                
                if (chunksPool.Count <= 0)
                    FillPool(1);
                
                var chunk = chunksPool.Dequeue();
                chunk.UseThisChunk(displayPosition, instant);
                
                if (!instant)
                    chunksProcessing.Enqueue(chunk);
                
                usedChunksMap.Add(displayPosition + finalOffset, chunk);
            }
        }

        private void SimpleCreate(int2 displayPosition)
        {
            var created = Instantiate(chunkPrefab, tf).GetComponent<Chunk>();
            usedChunks.Add(created);

            created.Initialize(blocksPerChunk);
            created.UseThisChunk(displayPosition);
            
            usedChunksMap.Add(created.chunkPosition, created);
        }

        private void FillPool(int count = 0)
        {
            int renderDistance = SettingsHolder.Instance.displayOptions.chunkRenderDistance;

            if (count == 0)
            {
                for (int z = 0; z < renderDistance; z++)
                for (int x = 0; x < renderDistance; x++)
                {
                    var created = Instantiate(chunkPrefab, tf).GetComponent<Chunk>();
                    created.name = "Chunk [InPool]";
                    chunksPool.Enqueue(created);

                    created.Initialize(blocksPerChunk);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    var created = Instantiate(chunkPrefab, tf).GetComponent<Chunk>();
                    created.name = "Chunk [InPool]";
                    chunksPool.Enqueue(created);

                    created.Initialize(blocksPerChunk);
                }
            }
            
        }
        
        private void OnApplicationQuit()
        {
            for (int i = 0; i < usedChunks.Count; i++)
                usedChunks[i].Dispose();

            for (int i = 0; i < chunksPool.Count; i++)
                chunksPool.Dequeue().Dispose();

            atlasMap.Dispose();
        }

        #region UsefulButNotCheapFunctions
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
            
            chunkPosition.x += initialOffset;
            chunkPosition.y += initialOffset;
            
            return worldData.chunks[chunkPosition].blocks[i];
        }
        

        //Get blocks in certain volume.
        public void GetBlocksVolume(int3 from, int3 to, NativeArray<Block> blocks)
        {
            int i = 0;
            
            //Debug.Log($"Volume from {from} to {to}");
            for (int y = from.y; y < to.y; y++)
            for (int z = from.z; z < to.z; z++)
            for (int x = from.x; x < to.x; x++)
            {
                blocks[i] = GetBlockAtPosition(new int3(x, y, z));
                i++;
            }
        }

        public void SetBlockAtPosition(int3 blockPosition, Block block)
        {
            int2 chunkPosition; 
            
            chunkPosition.x = Mathf.FloorToInt((0f + blockPosition.x) / width);
            chunkPosition.y = Mathf.FloorToInt((0f + blockPosition.z) / width);
            
            chunkPosition.x += initialOffset;
            chunkPosition.y += initialOffset;

            blockPosition.x = blockPosition.x >= 0 ? ((blockPosition.x % width + width) % width) : ((blockPosition.x % width + width) % width);
            blockPosition.z = blockPosition.z >= 0 ? ((blockPosition.z % width + width) % width) : ((blockPosition.z % width + width) % width);
            
            int i = blockPosition.x + blockPosition.z * width + blockPosition.y * widthSqr;
            worldData.chunks[chunkPosition].blocks[i] = block;

            if (usedChunksMap.ContainsKey(chunkPosition))
            {
                usedChunksMap[chunkPosition].Local(true);

                int2 setCheckPosition = int2.zero;
                if (blockPosition.x == 0)
                {
                    setCheckPosition = chunkPosition + new int2(-1, 0);
                    if (usedChunksMap.ContainsKey(setCheckPosition))
                        usedChunksMap[setCheckPosition].Local(true);
                }
                else if(blockPosition.x == width-1)
                {
                    setCheckPosition = chunkPosition + new int2(1, 0);
                    if (usedChunksMap.ContainsKey(setCheckPosition))
                        usedChunksMap[setCheckPosition].Local(true);
                }

                if (blockPosition.z == 0)
                {
                    setCheckPosition = chunkPosition + new int2(0, -1);
                    if (usedChunksMap.ContainsKey(setCheckPosition))
                        usedChunksMap[setCheckPosition].Local(true);
                }
                else if(blockPosition.z == width-1)
                {
                    setCheckPosition = chunkPosition + new int2(0, 1);
                    if (usedChunksMap.ContainsKey(setCheckPosition))
                        usedChunksMap[setCheckPosition].Local(true);
                }
            }
        }
        
        #endregion
    }
}