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
        public bool useJobSystem = true;
        
        public void UpdateGeometry(NativeArray<Block> blocks, Mesh mesh,
                                   NativeList<float3> vertices, NativeList<float3> normals, NativeList<int> triangles, NativeList<float2> uv)
        {
            var time = Time.realtimeSinceStartup;
            
            mesh.name = "chunkMesh";
            
            vertices.Clear();
            triangles.Clear();
            uv.Clear();
            
            GenerateMeshJob generateMeshJob = new GenerateMeshJob()
            {
                blocks = blocks,
                
                width  = SettingsHolder.Instance.proceduralGeneration.chunkWidth,
                height = SettingsHolder.Instance.proceduralGeneration.chunkHeight,
                
                vertices  = vertices,
                normals   = normals,
                triangles = triangles,
                uv        = uv
            };

            if (useJobSystem)
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
                                             new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1),
                                             new VertexAttributeDescriptor(VertexAttribute.TexCoord0, stream: 2, dimension: 2));
            
            var submeshDescriptor = new SubMeshDescriptor(0, triangles.Length, MeshTopology.Triangles);
            submeshDescriptor.firstVertex = 0;
            submeshDescriptor.vertexCount = vertices.Length;
            
            outputMeshData.subMeshCount = 1;
            outputMeshData.SetSubMesh(0, submeshDescriptor,
                                      MeshUpdateFlags.DontNotifyMeshUsers |
                                  MeshUpdateFlags.DontValidateIndices | 
                                  MeshUpdateFlags.DontResetBoneBounds | 
                                  MeshUpdateFlags.DontRecalculateBounds);
            
            WriteToMeshJob writeToMeshJob = new WriteToMeshJob()
            {
                outputMeshData = outputMeshData,

                vertices  = vertices,
                normals   = normals,
                triangles = triangles,
                uv        = uv
            };

            if (useJobSystem)
            {
                var writeToMeshHandle = writeToMeshJob.Schedule();
                writeToMeshHandle.Complete();
            }
            else
            {
                writeToMeshJob.Execute();
            }

            Mesh.ApplyAndDisposeWritableMeshData(outputMeshDataArray, mesh,
                                                 MeshUpdateFlags.DontNotifyMeshUsers |
                                                 MeshUpdateFlags.DontValidateIndices | 
                                                 MeshUpdateFlags.DontResetBoneBounds | 
                                                 MeshUpdateFlags.DontRecalculateBounds);
            
            //mesh.RecalculateNormals();
            //mesh.RecalculateTangents();
            //mesh.RecalculateBounds();
            
            //Debug.Log($"Geometry took: {Time.realtimeSinceStartup - time}");
        }
        
        [BurstCompile]
        private struct GenerateMeshJob : IJob
        {
            //TODO feed slightly bigger array of data.
            [ReadOnly] public NativeArray<Block> blocks;
            public int width, height;
            
            [WriteOnly] public NativeList<float3> vertices;
            [WriteOnly] public NativeList<float3> normals;
            [WriteOnly] public NativeList<int>    triangles;
            [WriteOnly] public NativeList<float2> uv;
            
            public void Execute()
            {
                int vCount = 0;
                
                float3 regularNormal = new float3(0, 0, -1);
                
                for (int y = 0; y < height; y++)
                for (int z = 0; z < width; z++)
                for (int x = 0; x < width; x++)
                {
                    int index = 0;
                    int widthSqr = width * width;
                    
                    index = x + z * width + y * widthSqr;
                    Block current = blocks[index];
                    Block top    = Block.Air;
                    Block bottom = Block.Air;
                    Block front  = Block.Air;
                    Block back   = Block.Air;
                    Block left   = Block.Air;
                    Block right  = Block.Air;
                    
                    if (current != Block.Air)
                    {
                        if (y < (height - 1))
                        {
                            index = x + z * width + (y + 1) * widthSqr;
                            top   = blocks[index];
                        }

                        if (y > 0)
                        {
                            index  = x + z * width + (y - 1) * widthSqr;
                            bottom = blocks[index];
                        }

                        if (z < (width - 1))
                        {
                            index = x + (z + 1) * width + y * widthSqr;
                            back = blocks[index];
                        }

                        if (z > 0)
                        {
                            index = x + (z - 1) * width + y * widthSqr;
                            front  = blocks[index];
                        }

                        if (x < (width - 1))
                        {
                            index = (x + 1) + z * width + y * widthSqr;
                            right = blocks[index];
                        }

                        if (x > 0)
                        {
                            index = (x - 1) + z * width + y * widthSqr;
                            left  = blocks[index];
                        }

                        float3 blockPos = new float3(x, y, z);
                        int fCount = 0;

                        if (top == Block.Air)
                        {
                            vertices.Add(blockPos + new float3(0, 1, 0));
                            vertices.Add(blockPos + new float3(0, 1, 1));
                            vertices.Add(blockPos + new float3(1, 1, 1));
                            vertices.Add(blockPos + new float3(1, 1, 0));
                            
                            normals.Add(new float3(0, 1, 0));
                            normals.Add(new float3(0, 1, 0));
                            normals.Add(new float3(0, 1, 0));
                            normals.Add(new float3(0, 1, 0));

                            uv.Add(new float2(.001f, .001f));
                            uv.Add(new float2(.001f, .0615f));
                            uv.Add(new float2(.0615f, .0615f));
                            uv.Add(new float2(.0615f, .001f));

                            vCount += 4;
                            fCount++;
                        }

                        if (bottom == Block.Air)
                        {
                            //Bottom
                            vertices.Add(blockPos + new float3(0, 0, 0));
                            vertices.Add(blockPos + new float3(1, 0, 0));
                            vertices.Add(blockPos + new float3(1, 0, 1));
                            vertices.Add(blockPos + new float3(0, 0, 1));
                            
                            normals.Add(new float3(0, -1, 1));
                            normals.Add(new float3(0, -1, 1));
                            normals.Add(new float3(0, -1, 1));
                            normals.Add(new float3(0, -1, 1));
                            
                            uv.Add(new float2(.001f, .001f));
                            uv.Add(new float2(.001f, .0615f));
                            uv.Add(new float2(.0615f, .0615f));
                            uv.Add(new float2(.0615f, .001f));

                            vCount += 4;
                            fCount++;
                        }

                        if (front == Block.Air)
                        {
                            vertices.Add(blockPos + new float3(0, 0, 0));
                            vertices.Add(blockPos + new float3(0, 1, 0));
                            vertices.Add(blockPos + new float3(1, 1, 0));
                            vertices.Add(blockPos + new float3(1, 0, 0));
                            
                            normals.Add(new float3(0, 0, -1));
                            normals.Add(new float3(0, 0, -1));
                            normals.Add(new float3(0, 0, -1));
                            normals.Add(new float3(0, 0, -1));

                            uv.Add(new float2(.001f, .001f));
                            uv.Add(new float2(.001f, .0615f));
                            uv.Add(new float2(.0615f, .0615f));
                            uv.Add(new float2(.0615f, .001f));

                            vCount += 4;
                            fCount++;
                        }

                        if (back == Block.Air)
                        {
                            vertices.Add(blockPos + new float3(1, 0, 1));
                            vertices.Add(blockPos + new float3(1, 1, 1));
                            vertices.Add(blockPos + new float3(0, 1, 1));
                            vertices.Add(blockPos + new float3(0, 0, 1));

                            normals.Add(new float3(0, 0, 1));
                            normals.Add(new float3(0, 0, 1));
                            normals.Add(new float3(0, 0, 1));
                            normals.Add(new float3(0, 0, 1));
                            
                            uv.Add(new float2(.001f, .001f));
                            uv.Add(new float2(.001f, .0615f));
                            uv.Add(new float2(.0615f, .0615f));
                            uv.Add(new float2(.0615f, .001f));

                            vCount += 4;
                            fCount++;
                        }
                        
                        if (right == Block.Air)
                        {
                            vertices.Add(blockPos + new float3(1, 0, 0));
                            vertices.Add(blockPos + new float3(1, 1, 0));
                            vertices.Add(blockPos + new float3(1, 1, 1));
                            vertices.Add(blockPos + new float3(1, 0, 1));

                            normals.Add(new float3(1, 0, 0));
                            normals.Add(new float3(1, 0, 0));
                            normals.Add(new float3(1, 0, 0));
                            normals.Add(new float3(1, 0, 0));
                            
                            uv.Add(new float2(.001f, .001f));
                            uv.Add(new float2(.001f, .0615f));
                            uv.Add(new float2(.0615f, .0615f));
                            uv.Add(new float2(.0615f, .001f));

                            vCount += 4;
                            fCount++;
                        }
                        
                        if (left == Block.Air)
                        {
                            vertices.Add(blockPos + new float3(0, 0, 1));
                            vertices.Add(blockPos + new float3(0, 1, 1));
                            vertices.Add(blockPos + new float3(0, 1, 0));
                            vertices.Add(blockPos + new float3(0, 0, 0));

                            normals.Add(new float3(-1, 0, 0));
                            normals.Add(new float3(-1, 0, 0));
                            normals.Add(new float3(-1, 0, 0));
                            normals.Add(new float3(-1, 0, 0));
                            
                            uv.Add(new float2(.001f, .001f));
                            uv.Add(new float2(.001f, .0615f));
                            uv.Add(new float2(.0615f, .0615f));
                            uv.Add(new float2(.0615f, .001f));

                            vCount += 4;
                            fCount++;
                        }

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
            
        }
        
        [BurstCompile]
        private struct WriteToMeshJob : IJob
        {
            [ReadOnly] public NativeList<float3> vertices;
            [ReadOnly] public NativeList<float3> normals;
            [ReadOnly] public NativeList<int>    triangles;
            [ReadOnly] public NativeList<float2> uv;

            public Mesh.MeshData outputMeshData;
            
            public void Execute()
            {
                var outputVerts = outputMeshData.GetVertexData<float3>();
                var outputNormals = outputMeshData.GetVertexData<float3>(1);
                var outputUVs   = outputMeshData.GetVertexData<float2>(stream: 2);
                
                var vCount = vertices.Length;
                var tCount = triangles.Length;

                for (var i = 0; i < vCount; i++)
                {
                    outputVerts[i] = vertices[i];
                    outputNormals[i] = normals[i];
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