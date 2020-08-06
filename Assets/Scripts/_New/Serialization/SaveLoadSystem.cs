using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using ProtoBuf;
using Unity.Mathematics;
using UnityCommunityVoxelProject.Terrain;
using Application = UnityEngine.Application;
using Ionic.Zlib;
using CompressionLevel = Ionic.Zlib.CompressionLevel;

namespace UnityCommunityVoxelProject.Serialization
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

            SaveWorld();
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
            }
        }

        private void SaveWorld()
        {
            WorldData worldData = ChunkManager.Instance.worldData;
            
            if (ChunkManager.Instance.worldData == null)
            {
                worldData  = new WorldData();
                worldData.chunks = new Dictionary<ProtoInt2, ChunkData>();
                
                ChunkManager.Instance.worldData = worldData;
            }

            float time = Time.realtimeSinceStartup;
            if (useCompression)
            {
                if (worldData.chunks.ContainsKey(new ProtoInt2(0, 0)))
                {
                    worldData.chunks[new ProtoInt2(0, 0)].blocks[0] = Block.Leaves;
                    Debug.Log(worldData.chunks[new ProtoInt2(0, 0)].blocks[0]);
                }
                

                var fileStream = File.Create(pathToSaveFiles + "Save.world");
                DeflateStream zlibStream = new DeflateStream(fileStream, CompressionMode.Compress, CompressionLevel.Default);
                Serializer.Serialize(zlibStream, worldData);
                
                zlibStream.Close();
                fileStream.Close();
            }
            else
            {
                if (worldData.chunks.ContainsKey(new ProtoInt2(0, 0)))
                {
                    worldData.chunks[new int2(0, 0)].blocks[0] = Block.Leaves;
                    Debug.Log(worldData.chunks[new ProtoInt2(0, 0)].blocks[0]);
                }
                
                var fileStream = File.Create(pathToSaveFiles + "Save.world");
                Serializer.Serialize(fileStream, worldData);
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
                var worldData = Serializer.Deserialize<WorldData>(protoStream);
                Debug.Log(worldData.chunks[new int2(0, 0)].blocks[0]);
                
                protoStream.Close();
                compressionStream.Close();
            }
            else
            {
                var fileStream = File.Open(pathToSaveFiles + "Save.world", FileMode.Open);
                var worldData  = Serializer.Deserialize<WorldData>(fileStream);
                ChunkManager.Instance.worldData = worldData;
                fileStream.Close();
                
                Debug.Log(worldData.chunks[new ProtoInt2(0, 0)].blocks[0]);
            }
            
            Debug.Log($"Loading took {Time.realtimeSinceStartup - time}s");
        }
    }

    [ProtoContract]
    public class WorldData
    {
        //TODO Think of file structure.
        [ProtoMember(1)] public Dictionary<ProtoInt2, ChunkData> chunks;
    }

    [ProtoContract]
    public class ChunkData
    {
        [ProtoMember(1)] public Block[] blocks;
    }
}