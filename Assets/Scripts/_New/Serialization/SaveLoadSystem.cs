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
            
            DataWorld dataWorld = ChunkManager.Instance.dataWorld;
            
            if (ChunkManager.Instance.dataWorld == null)
            {
                dataWorld        = new DataWorld();
                dataWorld.chunks = new Dictionary<int2, DataChunk>();
                
                ChunkManager.Instance.dataWorld = dataWorld;
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
            if (ChunkManager.Instance.dataWorld == null)
            {
                throw new Exception("There is no world data to save for some reason. ");
            }
            
            TerrainProceduralGeneration.Instance.CompleteAll();
            ProtoDataWorld protoDataWorld = ChunkManager.Instance.dataWorld;

            float time = Time.realtimeSinceStartup;
            if (useCompression)
            {
                if (protoDataWorld.chunks.ContainsKey(new ProtoInt2(0, 0)))
                {
                    protoDataWorld.chunks[new ProtoInt2(0, 0)].blocks[0] = Block.Leaves;
                    Debug.Log(protoDataWorld.chunks[new ProtoInt2(0, 0)].blocks[0]);
                }

                var fileStream = File.Create(pathToSaveFiles + "Save.world");
                DeflateStream zlibStream = new DeflateStream(fileStream, CompressionMode.Compress, CompressionLevel.Default);
                Serializer.Serialize(zlibStream, protoDataWorld);
                
                zlibStream.Close();
                fileStream.Close();
            }
            else
            {
                if (protoDataWorld.chunks.ContainsKey(new ProtoInt2(0, 0)))
                {
                    protoDataWorld.chunks[new int2(0, 0)].blocks[0] = Block.Leaves;
                    Debug.Log(protoDataWorld.chunks[new ProtoInt2(0, 0)].blocks[0]);
                }
                
                var fileStream = File.Create(pathToSaveFiles + "Save.world");
                Serializer.Serialize(fileStream, protoDataWorld);
                fileStream.Close();
            }
            
            Debug.Log($"Saving took {Time.realtimeSinceStartup - time}s");
        }

        private void LoadWorld()
        {
            float time = Time.realtimeSinceStartup;
            TerrainProceduralGeneration.Instance.CompleteAll();
            
            if (useCompression)
            {
                var fileStream = File.OpenRead(pathToSaveFiles + "Save.world");
                DeflateStream compressionStream = new DeflateStream(fileStream, CompressionMode.Decompress, CompressionLevel.Default);
                
                MemoryStream protoStream = new MemoryStream();
                compressionStream.CopyTo(protoStream);
                
                protoStream.Seek(0, SeekOrigin.Begin);
                var worldData = Serializer.Deserialize<ProtoDataWorld>(protoStream);

                ChunkManager.Instance.dataWorld = worldData;
                
                protoStream.Close();
                compressionStream.Close();
            }
            else
            {
                var fileStream = File.Open(pathToSaveFiles + "Save.world", FileMode.Open);
                var worldData  = Serializer.Deserialize<ProtoDataWorld>(fileStream);
                ChunkManager.Instance.dataWorld = worldData;
                fileStream.Close();
            }
            
            Debug.Log($"Loading took {Time.realtimeSinceStartup - time}s");
        }

    }
}