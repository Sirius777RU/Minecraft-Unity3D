using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityVoxelCommunityProject.Utility;

namespace UnityVoxelCommunityProject.Terrain
{
    [SelectionBase]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class Chunk : MonoBehaviour
    {
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;

        private NativeList<float3> vertices;
        private NativeList<float3> normals;
        private NativeList<int>    triangles;
        private NativeList<float2> uv;

        [HideInInspector] public int2 chunkPosition;
        private int3 volumeStart, volumeEnd;
        private int width, height, widthSqr;

        private Mesh mesh;
        private JobHandle meshGeneration;   
        
        [ReadOnly] private NativeArray<Block> currentChunk, rightChunk, leftChunk, backChunk, frontChunk;
        
        [HideInInspector] public ChunkProcessing currentStage = ChunkProcessing.NotStarted;
        [HideInInspector] public int framesInCurrentProcessingStage = 0;
        [HideInInspector] public bool readyForNextStage = false;
        [HideInInspector] public Transform tf;
        
        
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
            meshCollider.sharedMesh = mesh;
            meshCollider.cookingOptions = MeshColliderCookingOptions.None;

            width  = SettingsHolder.Instance.proceduralGeneration.chunkWidth;
            height = SettingsHolder.Instance.proceduralGeneration.chunkHeight;
            widthSqr = width * width;
            
            meshFilter.mesh.bounds = new Bounds(new Vector3((width + 2)/2, (height + 2)/2, (width + 2)/2) - new Vector3(1f, 0, 1f), 
                                                new Vector3((width + 2), (height + 2), (width + 2)));
            meshRenderer.enabled = false;
            meshCollider.enabled = false;
            gameObject.SetActive(false);
        }

        public void UseThisChunk(int2 position, bool instant = true)
        {
            tf.position = new Vector3(position.x * width, 0,  position.y * width);
            
            SetPosition(position);
            Local(instant);
            
            gameObject.SetActive(true);
        }

        public void FreeThisChunk()
        {
            meshRenderer.enabled = false;
            meshCollider.enabled = false;

            CompleteAllIfAny();

            gameObject.SetActive(false);
        }

        public void CompleteAllIfAny()
        {
            if (currentStage == ChunkProcessing.TerrainDataGeneration)
            {
                GrabChunksData();
            }
            if (currentStage == ChunkProcessing.MeshDataGeneration)
            {
                meshGeneration.Complete();
            }

            if (ChunksAnimator.Instance != null) ChunksAnimator.Instance.RemoveFromAnimation(this);
            currentStage = ChunkProcessing.NotStarted;
        }

        public void Local(bool instant)
        {
            gameObject.name = $"Chunk [x{chunkPosition.x} z{chunkPosition.y}]";

            if (instant)
            {
                InstantDisplay();
            }
            else
            {
                framesInCurrentProcessingStage++;
                Processing();
            }
        }

        private void InstantDisplay()
        {
            //Generate chunk with neighbors.
            TerrainProceduralGeneration.Instance.RequestChunkGeneration(chunkPosition, true);
            
            //Grab the result.
            GrabChunksData();
            
            //And use all data it to display mesh;
            TerrainGeometryGeneration.Instance.GenerateGeometry(mesh,
                                                                currentChunk, rightChunk, leftChunk,
                                                                frontChunk,
                                                                backChunk,
                                                                vertices, normals, triangles, uv).Schedule().Complete(); 
            
            TerrainGeometryGeneration.Instance.ApplyGeometry(mesh,
                                                             currentChunk, rightChunk, leftChunk,
                                                             frontChunk,
                                                             backChunk,
                                                             vertices, normals, triangles, uv); 
            
            //Then cause physics to update. That's not cheap, but out of other options here.
            //if(ChunkManager.Instance.updateColliders)
              //  meshCollider.sharedMesh = mesh;
            
            meshRenderer.enabled = true;
            if (ChunkManager.Instance.updateColliders)
            {
                if (meshCollider.enabled)
                {
                    meshCollider.sharedMesh = mesh;
                }
                else
                {
                    meshCollider.enabled = true;
                }
            }
        }

        private void Processing()
        {
            if (readyForNextStage)
            {
                if (currentStage == ChunkProcessing.NotStarted)
                {
                    currentStage                   = ChunkProcessing.TerrainDataGeneration;
                    framesInCurrentProcessingStage = 0;

                    TerrainProceduralGeneration.Instance.RequestChunkGeneration(chunkPosition,
                                                                                withNeighbors: true);
                }
                else if (currentStage == ChunkProcessing.TerrainDataGeneration)
                {
                    currentStage = ChunkProcessing.MeshDataGeneration;

                    GrabChunksData();
                    meshGeneration = TerrainGeometryGeneration.Instance.GenerateGeometry(mesh,
                                                                                         currentChunk, rightChunk,
                                                                                         leftChunk,
                                                                                         frontChunk,
                                                                                         backChunk,
                                                                                         vertices, normals, triangles,
                                                                                         uv).Schedule();
                }
                else if (currentStage == ChunkProcessing.MeshDataGeneration)
                {
                    currentStage = ChunkProcessing.Finished;
                    meshGeneration.Complete();

                    TerrainGeometryGeneration.Instance.ApplyGeometry(mesh,
                                                                     currentChunk, rightChunk, leftChunk,
                                                                     frontChunk,
                                                                     backChunk,
                                                                     vertices, normals, triangles, uv);

                    if (ChunkManager.Instance.updateColliders)
                        meshCollider.sharedMesh = mesh;

                    meshRenderer.enabled = true;
                    if(ChunkManager.Instance.updateColliders)
                        meshCollider.enabled = true;
                    
                    ChunksAnimator.Instance.Register(this);
                }

                readyForNextStage = false;
                framesInCurrentProcessingStage = 0;
            }
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

        private void SetPosition(int2 position)
        {
            int  initialOffset = ChunkManager.Instance.initialOffset;
            int2 offset        = new int2(initialOffset, initialOffset);
            
            chunkPosition = new int2(position.x + offset.x, position.y + offset.y);
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
            if(TerrainProceduralGeneration.Instance == null)
                return;
                
            TerrainProceduralGeneration.Instance.Complete(chunkPosition, withNeighbors: true);

            currentChunk = ChunkManager.Instance.dataWorld.chunks[chunkPosition].blocks;
            rightChunk   = ChunkManager.Instance.dataWorld.chunks[chunkPosition + new int2(1, 0)].blocks;
            leftChunk    = ChunkManager.Instance.dataWorld.chunks[chunkPosition + new int2(-1, 0)].blocks;
            frontChunk   = ChunkManager.Instance.dataWorld.chunks[chunkPosition + new int2(0, 1)].blocks;
            backChunk    = ChunkManager.Instance.dataWorld.chunks[chunkPosition + new int2(0, -1)].blocks;
        }

        private void OnDestroy()
        {
            CompleteAllIfAny();
            Dispose();
        }

        public void Dispose()
        {
            vertices.Dispose();
            normals.Dispose();
            triangles.Dispose();
            uv.Dispose();
        }
    }

    public enum ChunkProcessing
    {
        NotStarted,
        TerrainDataGeneration, MeshDataGeneration,
        Finished
    }
}

