using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using ProtoBuf;
using Unity.Mathematics;
using Application = UnityEngine.Application;
using Ionic.Zlib;
using Unity.Collections;
using UnityVoxelCommunityProject.Terrain;
using CompressionLevel = Ionic.Zlib.CompressionLevel;

namespace UnityVoxelCommunityProject.Serialization
{
    public class SaveLoadSystem : Singleton<SaveLoadSystem>
    {
        public bool useCompression = true;
        public CompressionLevel compressionLevel = CompressionLevel.Default;
        
        private string pathToSaveFiles;
        
        public void Initialize()
        {
            pathToSaveFiles = Application.persistentDataPath + "/Save/";
            if (!Directory.Exists(pathToSaveFiles))
            {
                Debug.Log($"Creating folder for save files at \"{pathToSaveFiles}\".");
                Directory.CreateDirectory(pathToSaveFiles);
            }
            
            WorldData worldData = ChunkManager.Instance.worldData;
            
            if (ChunkManager.Instance.worldData == null)
            {
                worldData        = new WorldData();
                worldData.chunks = new Dictionary<int2, ChunkData>();
                
                ChunkManager.Instance.worldData = worldData;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                SaveWorld();
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                LoadWorld();
                ChunkManager.Instance.UpdateChunks();
            }
        }

        private void SaveWorld()
        {
            if (ChunkManager.Instance.worldData == null)
            {
                throw new Exception("There is no world data to save for some reason. ");
            }
            
            ProtoWorldData protoWorldData = ChunkManager.Instance.worldData;

            float time = Time.realtimeSinceStartup;
            if (useCompression)
            {
                if (protoWorldData.chunks.ContainsKey(new ProtoInt2(0, 0)))
                {
                    protoWorldData.chunks[new ProtoInt2(0, 0)].blocks[0] = Block.Leaves;
                    Debug.Log(protoWorldData.chunks[new ProtoInt2(0, 0)].blocks[0]);
                }

                var fileStream = File.Create(pathToSaveFiles + "Save.world");
                DeflateStream zlibStream = new DeflateStream(fileStream, CompressionMode.Compress, CompressionLevel.Default);
                Serializer.Serialize(zlibStream, protoWorldData);
                
                zlibStream.Close();
                fileStream.Close();
            }
            else
            {
                if (protoWorldData.chunks.ContainsKey(new ProtoInt2(0, 0)))
                {
                    protoWorldData.chunks[new int2(0, 0)].blocks[0] = Block.Leaves;
                    Debug.Log(protoWorldData.chunks[new ProtoInt2(0, 0)].blocks[0]);
                }
                
                var fileStream = File.Create(pathToSaveFiles + "Save.world");
                Serializer.Serialize(fileStream, protoWorldData);
                fileStream.Close();
            }
            
            Debug.Log($"Saving took {Time.realtimeSinceStartup - time}s");
        }

        private void LoadWorld()
        {
            float time = Time.realtimeSinceStartup;
            
            if (useCompression)
            {
                var fileStream = File.OpenRead(pathToSaveFiles + "Save.world");
                DeflateStream compressionStream = new DeflateStream(fileStream, CompressionMode.Decompress, CompressionLevel.Default);
                
                MemoryStream protoStream = new MemoryStream();
                compressionStream.CopyTo(protoStream);
                
                protoStream.Seek(0, SeekOrigin.Begin);
                var worldData = Serializer.Deserialize<ProtoWorldData>(protoStream);

                ChunkManager.Instance.worldData = worldData;
                
                protoStream.Close();
                compressionStream.Close();
            }
            else
            {
                var fileStream = File.Open(pathToSaveFiles + "Save.world", FileMode.Open);
                var worldData  = Serializer.Deserialize<ProtoWorldData>(fileStream);
                ChunkManager.Instance.worldData = worldData;
                fileStream.Close();
            }
            
            Debug.Log($"Loading took {Time.realtimeSinceStartup - time}s");
        }

    }

    public class WorldData
    {
        public Dictionary<int2, ChunkData> chunks;
    }

    public class ChunkData
    {
        public NativeArray<Block> blocks;
    }
    
    [ProtoContract]
    public class ProtoWorldData
    {
        //TODO Think of file structure.
        [ProtoMember(1)] public Dictionary<ProtoInt2, ProtoChunkData> chunks;
        
        public static implicit operator WorldData(ProtoWorldData p)
        {
            var result = new WorldData()
            {
                chunks = new Dictionary<int2, ChunkData>()
            };

            var keys = p.chunks.Keys.ToArray();
            var values = p.chunks.Values.ToArray();
            
            for (int i = 0; i < p.chunks.Count; i++)
            {
                result.chunks.Add(keys[i], values[i]);
            }

            return result;
        }

        public static implicit operator ProtoWorldData(WorldData p)
        {
            var result = new ProtoWorldData()
            {
                chunks = new Dictionary<ProtoInt2, ProtoChunkData>()
            };

            var keys   = p.chunks.Keys.ToArray();
            var values = p.chunks.Values.ToArray();
            
            for (int i = 0; i < p.chunks.Count; i++)
            {
                result.chunks.Add(keys[i], values[i]);
            }

            return result;
        }
    }

    [ProtoContract]
    public class ProtoChunkData
    {
        [ProtoMember(1)] public Block[] blocks;

        public static implicit operator ChunkData(ProtoChunkData p)
        {
            return new ChunkData()
            {
                blocks = new NativeArray<Block>(p.blocks, Allocator.Persistent)
            };
        }

        public static implicit operator ProtoChunkData(ChunkData p)
        {
            var result = new ProtoChunkData()
            {
                blocks = new Block[p.blocks.Length]
            };
            p.blocks.CopyTo(result.blocks);

            return result;
        }
    }
}