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
        
        private NativeList<float3> vertices;
        private NativeList<int>    triangles;
        private NativeList<float2> uv;
    
        public void Initialize(int blocksCount)
        {
            blocks    = new NativeArray<Block>(blocksCount, Allocator.Persistent);
            
            vertices  = new NativeList<float3>(393300, Allocator.Persistent);
            triangles = new NativeList<int>(590004, Allocator.Persistent); 
            uv        = new NativeList<float2>(393300, Allocator.Persistent);

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
            //TerrainProceduralGeneration.Instance.GenerateChunk(blocks);
            
            ChunksGeometryGeneration.Instance.UpdateGeometry(blocks, meshFilter.mesh,
                                                             vertices, triangles, uv);

            
            meshCollider.enabled = false;
            meshCollider.enabled = true;
        }

        public void Dispose()
        {
            blocks.Dispose();

            vertices.Dispose();
            triangles.Dispose();
            uv.Dispose();
        }
    }
}

