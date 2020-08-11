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
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private Transform tf;
        
        private NativeList<float3> vertices;
        private NativeList<float3> normals;
        private NativeList<int>    triangles;
        private NativeList<float2> uv;

        [HideInInspector] public int2 chunkPosition;
        private int3 volumeStart, volumeEnd;
        private int width, height, widthSqr;

        private Mesh mesh;

        [ReadOnly] private NativeArray<Block> currentChunk, rightChunk, leftChunk, backChunk, frontChunk;
        
    
        public void Initialize(int blocksCount)
        {
            vertices  = new NativeList<float3>(10000, Allocator.Persistent);
            normals   = new NativeList<float3>(10000, Allocator.Persistent);
            triangles = new NativeList<int>(15000, Allocator.Persistent); 
            uv        = new NativeList<float2>(10000, Allocator.Persistent);

            tf = GetComponent<Transform>();
            meshFilter   = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();

            mesh = new Mesh();
            mesh.MarkDynamic();
            meshFilter.mesh = mesh;
            meshCollider.cookingOptions = MeshColliderCookingOptions.None;

            width  = SettingsHolder.Instance.proceduralGeneration.chunkWidth;
            height = SettingsHolder.Instance.proceduralGeneration.chunkHeight;
            widthSqr = width * width;
            
            meshFilter.mesh.bounds = new Bounds(new Vector3((width + 2)/2, (height + 2)/2, (width + 2)/2), new Vector3((width + 2), (height + 2), (width + 2)));
        }

        public void Local()
        {
            RecalculatePosition();

            //Generate chunk with neighbors.
            TerrainProceduralGeneration.Instance.RequestChunkGeneration(chunkPosition, withNeighbors: true);
            
            //Grab the result.
            GrabChunksData();
            
            //And use all data it to display mesh;
            ChunksGeometryGeneration.Instance.UpdateGeometry(mesh, 
                                                             currentChunk, rightChunk, leftChunk, frontChunk, backChunk, 
                                                             vertices, normals, triangles, uv); 
            
            //Then cause physics to update. That's not cheap, but out of other options here.
            if(ChunkManager.Instance.updateColliders)
                meshCollider.sharedMesh = mesh;
        }

        private void RecalculatePosition()
        {
            int x = Mathf.FloorToInt(tf.position.x / width);
            int z = Mathf.FloorToInt(tf.position.z / width);

            //Add custom offset.
            int  initialOffset = ChunkManager.Instance.initialOffset;
            int2 offset        = new int2(initialOffset, initialOffset);
            
            chunkPosition = new int2(x + offset.x, z + offset.y);

        }

        private void RecalculateVolume()
        {
            int2 from = (chunkPosition * width);
            int2 to   = (chunkPosition * width) + new int2(width, width);
            
            volumeStart = new int3(from.x, 0, from.y);
            volumeEnd   = new int3(to.x, height, to.y);
        }

        private void GrabChunksData()
        {
            currentChunk = ChunkManager.Instance.worldData.chunks[chunkPosition].blocks;
            rightChunk   = ChunkManager.Instance.worldData.chunks[chunkPosition + new int2(1, 0)].blocks;
            leftChunk    = ChunkManager.Instance.worldData.chunks[chunkPosition + new int2(-1, 0)].blocks;
            frontChunk   = ChunkManager.Instance.worldData.chunks[chunkPosition + new int2(0, 1)].blocks;
            backChunk    = ChunkManager.Instance.worldData.chunks[chunkPosition + new int2(0, -1)].blocks;
            
        }

        public void Dispose()
        {
            vertices.Dispose();
            normals.Dispose();
            triangles.Dispose();
            uv.Dispose();
        }
    }
}

