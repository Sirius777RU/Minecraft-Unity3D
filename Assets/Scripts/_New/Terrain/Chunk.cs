using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityCommunityVoxelProject.Utility;

namespace UnityCommunityVoxelProject.Terrain
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
            
            float width  = SettingsHolder.Instance.proceduralGeneration.chunkWidth;
            float height = SettingsHolder.Instance.proceduralGeneration.chunkHeight;
            
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
            
            TerrainProceduralGeneration.Instance.GenerateChunk(blocks, chunkPosition);
            
            ChunksGeometryGeneration.Instance.UpdateGeometry(blocks, meshFilter.mesh,
                                                             vertices, normals, triangles, uv);

            //meshCollider.sharedMesh = meshFilter.mesh;
        }

        private void RecalculatePosition()
        {
            int width  = SettingsHolder.Instance.proceduralGeneration.chunkWidth;
            int height = SettingsHolder.Instance.proceduralGeneration.chunkHeight;

            int x = Mathf.FloorToInt(tf.position.x / width) * width;
            int z = Mathf.FloorToInt(tf.position.z / width) * width;

            chunkPosition = new int2(x, z);
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

