using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityVoxelCommunityProject.Utility;

namespace UnityVoxelCommunityProject.Terrain
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class Chunk : MonoBehaviour
    {
        private NativeArray<Block> blocks;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private Transform tf;
        
        private NativeList<float3> vertices;
        private NativeList<float3> normals;
        private NativeList<int>    triangles;
        private NativeList<float2> uv;

        private int2 chunkPosition;
        private int3 volumeStart, volumeEnd;
        private int width, height, widthSqr;
    
        public void Initialize(int blocksCount)
        {
            blocks    = new NativeArray<Block>(blocksCount, Allocator.Persistent);
            
            vertices  = new NativeList<float3>(15000, Allocator.Persistent);
            normals   = new NativeList<float3>(15000, Allocator.Persistent);
            triangles = new NativeList<int>(25000, Allocator.Persistent); 
            uv        = new NativeList<float2>(15000, Allocator.Persistent);

            tf = GetComponent<Transform>();
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
            
            meshFilter.mesh = new Mesh();
            meshCollider.sharedMesh = meshFilter.mesh;
            meshFilter.mesh.MarkDynamic();
            
            width  = SettingsHolder.Instance.proceduralGeneration.chunkWidth;
            height = SettingsHolder.Instance.proceduralGeneration.chunkHeight;
            widthSqr = width * width;
            
            meshFilter.mesh.bounds = new Bounds(new Vector3(width/2, height/2, width/2), new Vector3(width, height, width));
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Local();
            }
        }

        public void Local()
        {
            RecalculatePosition();
            
            TerrainProceduralGeneration.Instance.GenerateChunk(chunkPosition, withNeighbors: true);
            
            ChunkManager.Instance.GetBlocksVolume(volumeStart, volumeEnd, blocks);
            
            ChunksGeometryGeneration.Instance.UpdateGeometry(blocks, meshFilter.mesh,
                                                             vertices, normals, triangles, uv);

            meshCollider.sharedMesh = meshFilter.mesh;
        }

        private void RecalculatePosition()
        {
            int x = Mathf.FloorToInt(tf.position.x / width);
            int z = Mathf.FloorToInt(tf.position.z / width);

            chunkPosition = new int2(x, z);
            
            int2 from = ((chunkPosition * 16) + new int2(-1, -1));
            int2 to   = ((chunkPosition * 16) + new int2(17, 17));
            
            volumeStart = new int3(from.x, 0, from.y);
            volumeEnd = new int3(to.x, height, to.y);
            //Debug.Log(chunkPosition);
        }

        public void Dispose()
        {
            blocks.Dispose();

            vertices.Dispose();
            normals.Dispose();
            triangles.Dispose();
            uv.Dispose();
        }
    }
}

