using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityCommunityVoxelProject.Utility;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityCommunityVoxelProject.Terrain
{
    public class ChunksGeometryGeneration : Singleton<ChunksGeometryGeneration>
    {
        public void UpdateGeometry(NativeArray<Block> blocks, Mesh mesh,
                                   NativeList<float3> vertices, NativeList<int> triangles, NativeList<float2> uv)
        {
            var time = Time.realtimeSinceStartup;
            bool useJobs = ChunkManager.Instance.useJobSystem;
            
            mesh.name = "Poop";
            
            vertices.Clear();
            triangles.Clear();
            uv.Clear();
            
            GenerateMeshJob generateMeshJob = new GenerateMeshJob()
            {
                width  = SettingsHolder.Instance.proceduralGeneration.chunkWidth,
                height = SettingsHolder.Instance.proceduralGeneration.chunkHeight,
                
                vertices  = vertices,
                triangles = triangles,
                uv        = uv
            };

            if (useJobs)
            {
                var generationHandle = generateMeshJob.Schedule();
                generationHandle.Complete();
            }
            else
            {
                generateMeshJob.Execute();
            }

            var outputMeshDataArray = Mesh.AllocateWritableMeshData(1);
            var outputMeshData = outputMeshDataArray[0];
            outputMeshData.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);
            outputMeshData.SetVertexBufferParams(vertices.Length,
                                             new VertexAttributeDescriptor(VertexAttribute.Position),
                                             new VertexAttributeDescriptor(VertexAttribute.TexCoord0, stream: 1, dimension: 2));
            
            var submeshDescriptor = new SubMeshDescriptor(0, triangles.Length, MeshTopology.Triangles);
            submeshDescriptor.firstVertex = 0;
            submeshDescriptor.vertexCount = vertices.Length;
            
            outputMeshData.subMeshCount = 1;
            outputMeshData.SetSubMesh(0, submeshDescriptor,  
                                  MeshUpdateFlags.DontValidateIndices | 
                                  MeshUpdateFlags.DontResetBoneBounds | 
                                  MeshUpdateFlags.DontRecalculateBounds);
            
            WriteToMeshJob writeToMeshJob = new WriteToMeshJob()
            {
                outputMeshData = outputMeshData,
                
                vertices = vertices,
                triangles = triangles,
                uv = uv
            };

            if (useJobs)
            {
                var writeToMeshHandle = writeToMeshJob.Schedule();
                writeToMeshHandle.Complete();
            }
            else
            {
                writeToMeshJob.Execute();
            }

            Mesh.ApplyAndDisposeWritableMeshData(outputMeshDataArray, mesh, 
                                                 MeshUpdateFlags.DontValidateIndices | 
                                                 MeshUpdateFlags.DontResetBoneBounds | 
                                                 MeshUpdateFlags.DontRecalculateBounds);
            
            //mesh.RecalculateTangents();
            //mesh.RecalculateNormals();
            //mesh.RecalculateBounds();
            
            Debug.Log($"Took: {Time.realtimeSinceStartup - time}");
        }
        
        [BurstCompile]
        private struct GenerateMeshJob : IJob
        {
            public int width, height;
            
            [WriteOnly] public NativeList<float3> vertices;
            [WriteOnly] public NativeList<int>    triangles;
            [WriteOnly] public NativeList<float2> uv;
            
            public void Execute()
            {
                int vCount = 0;
                
                for (int x = 1; x < width + 1; x++)
                for (int z = 1; z < width + 1; z++)
                for (int y = 0; y < height; y++)
                {
                    float3 blockPos = new float3(x - 1, y, z - 1);
                    int fCount = 0;

                    //Top
                    vertices.Add(blockPos + new float3(0, 1, 0));
                    vertices.Add(blockPos + new float3(0, 1, 1));
                    vertices.Add(blockPos + new float3(1, 1, 1));
                    vertices.Add(blockPos + new float3(1, 1, 0));

                    uv.Add(new float2(.001f, .001f));
                    uv.Add(new float2(.001f, .0615f));
                    uv.Add(new float2(.0615f, .0615f));
                    uv.Add(new float2(.0615f, .001f));

                    vCount += 4;
                    fCount++;

                    //Bottom
                    vertices.Add(blockPos + new float3(0, 0, 0));
                    vertices.Add(blockPos + new float3(1, 0, 0));
                    vertices.Add(blockPos + new float3(1, 0, 1));
                    vertices.Add(blockPos + new float3(0, 0, 1));

                    uv.Add(new float2(.001f, .001f));
                    uv.Add(new float2(.001f, .0615f));
                    uv.Add(new float2(.0615f, .0615f));
                    uv.Add(new float2(.0615f, .001f));

                    vCount += 4;
                    fCount++;

                    //Front
                    vertices.Add(blockPos + new float3(0, 0, 0));
                    vertices.Add(blockPos + new float3(0, 1, 0));
                    vertices.Add(blockPos + new float3(1, 1, 0));
                    vertices.Add(blockPos + new float3(1, 0, 0));

                    uv.Add(new float2(.001f, .001f));
                    uv.Add(new float2(.001f, .0615f));
                    uv.Add(new float2(.0615f, .0615f));
                    uv.Add(new float2(.0615f, .001f));

                    vCount += 4;
                    fCount++;

                    //Back
                    vertices.Add(blockPos + new float3(1, 0, 1));
                    vertices.Add(blockPos + new float3(1, 1, 1));
                    vertices.Add(blockPos + new float3(0, 1, 1));
                    vertices.Add(blockPos + new float3(0, 0, 1));

                    uv.Add(new float2(.001f, .001f));
                    uv.Add(new float2(.001f, .0615f));
                    uv.Add(new float2(.0615f, .0615f));
                    uv.Add(new float2(.0615f, .001f));

                    vCount += 4;
                    fCount++;

                    //Left
                    vertices.Add(blockPos + new float3(0, 0, 1));
                    vertices.Add(blockPos + new float3(0, 1, 1));
                    vertices.Add(blockPos + new float3(0, 1, 0));
                    vertices.Add(blockPos + new float3(0, 0, 0));

                    uv.Add(new float2(.001f, .001f));
                    uv.Add(new float2(.001f, .0615f));
                    uv.Add(new float2(.0615f, .0615f));
                    uv.Add(new float2(.0615f, .001f));

                    vCount += 4;
                    fCount++;

                    //Right
                    vertices.Add(blockPos + new float3(1, 0, 0));
                    vertices.Add(blockPos + new float3(1, 1, 0));
                    vertices.Add(blockPos + new float3(1, 1, 1));
                    vertices.Add(blockPos + new float3(1, 0, 1));

                    uv.Add(new float2(.001f, .001f));
                    uv.Add(new float2(.001f, .0615f));
                    uv.Add(new float2(.0615f, .0615f));
                    uv.Add(new float2(.0615f, .001f));

                    vCount += 4;
                    fCount++;


                    int tl = vCount - 4 * fCount;
                    for (int i = 0; i < fCount; i++)
                    {
                        triangles.Add(tl + i * 4);
                        triangles.Add(tl + i * 4 + 1);
                        triangles.Add(tl + i * 4 + 2);

                        triangles.Add(tl + i * 4);
                        triangles.Add(tl + i * 4 + 2);
                        triangles.Add(tl + i * 4 + 3);
                    }
                }
            }
            
        }
        
        [BurstCompile]
        private struct WriteToMeshJob : IJob
        {
            [ReadOnly] public NativeList<float3> vertices;
            [ReadOnly] public NativeList<int>    triangles;
            [ReadOnly] public NativeList<float2> uv;

            public Mesh.MeshData outputMeshData;
            
            public void Execute()
            {
                var outputVerts = outputMeshData.GetVertexData<float3>();
                var outputUVs   = outputMeshData.GetVertexData<Vector2>(stream: 1);
                
                var vCount = vertices.Length;
                var tCount = triangles.Length;

                for (var i = 0; i < vCount; i++)
                {
                    outputVerts[i] = vertices[i];
                    outputUVs[i] = uv[i];
                }
                
                if (outputMeshData.indexFormat == IndexFormat.UInt16)
                {
                    var outputTriangles = outputMeshData.GetIndexData<ushort>();
                    for (var i = 0; i < tCount; i++)
                        outputTriangles[i] = (ushort) triangles[i];
                }
                else
                {
                    var outputTriangles = outputMeshData.GetIndexData<int>();
                    for (var i = 0; i < tCount; i++)
                        outputTriangles[i] = triangles[i];
                }
            }
        }
    }
}