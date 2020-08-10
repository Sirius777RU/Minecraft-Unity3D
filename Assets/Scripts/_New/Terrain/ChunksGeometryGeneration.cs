using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityVoxelCommunityProject.Utility;

namespace UnityVoxelCommunityProject.Terrain
{
    public class ChunksGeometryGeneration : Singleton<ChunksGeometryGeneration>
    {
        public IndexFormat indexFormat = IndexFormat.UInt16;
        public bool useJobSystem = true;

        public void UpdateGeometry(Mesh mesh,
                                   NativeArray<Block> currentChunk,
                                   NativeArray<Block> rightChunk, NativeArray<Block> leftChunk,
                                   NativeArray<Block> frontChunk, NativeArray<Block> backChunk,
                                   
                                   NativeList<float3> vertices,
                                   NativeList<float3> normals,
                                   NativeList<int> triangles, NativeList<float2> uv)
        {
            var time = Time.realtimeSinceStartup;
            
            mesh.name = "chunkMesh";
            
            vertices.Clear();
            triangles.Clear();
            normals.Clear();
            uv.Clear();
            
            GenerateMeshJob generateMeshJob = new GenerateMeshJob()
            {
                currentChunk = currentChunk,
                
                leftChunk  = leftChunk, frontChunk = frontChunk,
                rightChunk = rightChunk, backChunk = backChunk,
                
                atlasMap = ChunkManager.Instance.atlasMap,
                
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
            outputMeshData.SetIndexBufferParams(triangles.Length, indexFormat);
            outputMeshData.SetVertexBufferParams(vertices.Length,
                                             new VertexAttributeDescriptor(VertexAttribute.Position),
                                             new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1),
                                             new VertexAttributeDescriptor(VertexAttribute.TexCoord0, stream: 2, dimension: 2));
            
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
                                                 MeshUpdateFlags.DontValidateIndices |
                                                 MeshUpdateFlags.DontResetBoneBounds |
                                                 MeshUpdateFlags.DontRecalculateBounds);
            
            //mesh.RecalculateNormals();
            //mesh.RecalculateTangents();
            //mesh.RecalculateBounds();
            
            //Debug.Log($"Geometry took: {Time.realtimeSinceStartup - time}");
        }
        
        [BurstCompile(FloatPrecision.Low, FloatMode.Fast, CompileSynchronously = true)]
        private struct GenerateMeshJob : IJob
        {
            [ReadOnly] public NativeArray<Block> currentChunk, 
                                                 leftChunk, rightChunk, frontChunk, backChunk;
            
            [NativeDisableContainerSafetyRestriction][ReadOnly] public NativeArray<int2> atlasMap;
            public int width, height;
            
            [WriteOnly] public NativeList<float3> vertices;
            [WriteOnly] public NativeList<float3> normals;
            [WriteOnly] public NativeList<int>    triangles;
            [WriteOnly] public NativeList<float2> uv;
            
            public unsafe void Execute()
            {
                int vCount = 0;
                
                for (int y = 0; y < height; y++)
                for (int z = 0; z < width; z++)
                for (int x = 0; x < width; x++)
                {
                    int index = 0;
                    int widthSqr = width * width;

                    index = x + z * width + y * widthSqr;
                    Block current = currentChunk[index];
                    Block top     = Block.Air;
                    Block bottom  = Block.Air;
                    Block back    = Block.Air;
                    Block front   = Block.Air;
                    Block left    = Block.Air;
                    Block right   = Block.Air;
                    
                    if (current != Block.Air)
                    {
                        if (y < (height - 1))
                        {
                            index = x + z * width + (y + 1) * widthSqr;
                            top   = currentChunk[index];
                        }

                        //There is no need to draw bottom of chunk - it's should be always invisible.
                        if (y <= 0)
                        {
                            bottom = Block.Core;
                        }
                        else
                        {
                            index  = x + z * width + (y - 1) * widthSqr;
                            bottom = currentChunk[index];
                        }

                        if (z > 0)
                        {
                            index = x + (z - 1) * width + y * widthSqr;
                            back = currentChunk[index];
                        }
                        else
                        {
                            index = x + (width - 1) * width + y * widthSqr;
                            back  = backChunk[index];
                        }
                        
                        if (z < (width - 1))
                        {
                            index = x + (z + 1) * width + y * widthSqr;
                            front  = currentChunk[index];
                        }
                        else
                        {
                            index = x + (0) * width + y * widthSqr;
                            front = frontChunk[index];
                        }
                        
                        if (x > 0)
                        {
                            index = (x - 1) + z * width + y * widthSqr;
                            left  = currentChunk[index];
                        }
                        else
                        {
                            index = (width - 1) + z * width + y * widthSqr;
                            left = leftChunk[index];
                        }

                        if (x < (width - 1))
                        {
                            index = (x + 1) + z * width + y * widthSqr;
                            right = currentChunk[index];
                        }
                        else
                        {
                            index = (0) + z * width + y * widthSqr;
                            right = rightChunk[index];
                        }
                        
                        float3 blockPos = new float3(x - 1, y - 1, z - 1);
                        float2 blockUV;
                        int fCount = 0;
                        
                        if (!Opaque(current, top))
                        {
                            vertices.Add(blockPos + new float3(0, 1, 0));
                            vertices.Add(blockPos + new float3(0, 1, 1));
                            vertices.Add(blockPos + new float3(1, 1, 1));
                            vertices.Add(blockPos + new float3(1, 1, 0));
                            
                            normals.Add(new float3(0, 1, 0));
                            normals.Add(new float3(0, 1, 0));
                            normals.Add(new float3(0, 1, 0));
                            normals.Add(new float3(0, 1, 0));

                            blockUV = atlasMap[(((int) current) * 3)];
                            uv.Add(new float2(blockUV.x / 16f + .001f, blockUV.y / 16f + .001f));
                            uv.Add(new float2(blockUV.x / 16f + .001f, (blockUV.y + 1) / 16f - .001f));
                            uv.Add(new float2((blockUV.x + 1) / 16f - .001f, (blockUV.y + 1) / 16f - .001f));
                            uv.Add(new float2((blockUV.x + 1) / 16f - .001f, blockUV.y / 16f + .001f));

                            vCount += 4;
                            fCount++;
                        }

                        if (!Opaque(current, bottom))
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
                            
                            blockUV = atlasMap[(((int) current) * 3) + 2];
                            uv.Add(new float2(blockUV.x / 16f + .001f, blockUV.y / 16f + .001f));
                            uv.Add(new float2(blockUV.x / 16f + .001f, (blockUV.y + 1) / 16f - .001f));
                            uv.Add(new float2((blockUV.x + 1) / 16f - .001f, (blockUV.y + 1) / 16f - .001f));
                            uv.Add(new float2((blockUV.x + 1) / 16f - .001f, blockUV.y / 16f + .001f));

                            vCount += 4;
                            fCount++;
                        }

                        if (!Opaque(current, back))
                        {
                            vertices.Add(blockPos + new float3(0, 0, 0));
                            vertices.Add(blockPos + new float3(0, 1, 0));
                            vertices.Add(blockPos + new float3(1, 1, 0));
                            vertices.Add(blockPos + new float3(1, 0, 0));
                            
                            normals.Add(new float3(0, 0, -1));
                            normals.Add(new float3(0, 0, -1));
                            normals.Add(new float3(0, 0, -1));
                            normals.Add(new float3(0, 0, -1));

                            blockUV = atlasMap[(((int) current) * 3) + 1];
                            uv.Add(new float2(blockUV.x / 16f + .001f, blockUV.y / 16f + .001f));
                            uv.Add(new float2(blockUV.x / 16f + .001f, (blockUV.y + 1) / 16f - .001f));
                            uv.Add(new float2((blockUV.x + 1) / 16f - .001f, (blockUV.y + 1) / 16f - .001f));
                            uv.Add(new float2((blockUV.x + 1) / 16f - .001f, blockUV.y / 16f + .001f));

                            vCount += 4;
                            fCount++;
                        }

                        if (!Opaque(current, front))
                        {
                            vertices.Add(blockPos + new float3(1, 0, 1));
                            vertices.Add(blockPos + new float3(1, 1, 1));
                            vertices.Add(blockPos + new float3(0, 1, 1));
                            vertices.Add(blockPos + new float3(0, 0, 1));

                            normals.Add(new float3(0, 0, 1));
                            normals.Add(new float3(0, 0, 1));
                            normals.Add(new float3(0, 0, 1));
                            normals.Add(new float3(0, 0, 1));
                            
                            blockUV = atlasMap[(((int) current) * 3) + 1];
                            uv.Add(new float2(blockUV.x / 16f + .001f, blockUV.y / 16f + .001f));
                            uv.Add(new float2(blockUV.x / 16f + .001f, (blockUV.y + 1) / 16f - .001f));
                            uv.Add(new float2((blockUV.x + 1) / 16f - .001f, (blockUV.y + 1) / 16f - .001f));
                            uv.Add(new float2((blockUV.x + 1) / 16f - .001f, blockUV.y / 16f + .001f));

                            vCount += 4;
                            fCount++;
                        }
                        
                        if (!Opaque(current, right))
                        {
                            vertices.Add(blockPos + new float3(1, 0, 0));
                            vertices.Add(blockPos + new float3(1, 1, 0));
                            vertices.Add(blockPos + new float3(1, 1, 1));
                            vertices.Add(blockPos + new float3(1, 0, 1));

                            normals.Add(new float3(1, 0, 0));
                            normals.Add(new float3(1, 0, 0));
                            normals.Add(new float3(1, 0, 0));
                            normals.Add(new float3(1, 0, 0));
                            
                            blockUV = atlasMap[(((int) current) * 3) + 1];
                            uv.Add(new float2(blockUV.x / 16f + .001f, blockUV.y / 16f + .001f));
                            uv.Add(new float2(blockUV.x / 16f + .001f, (blockUV.y + 1) / 16f - .001f));
                            uv.Add(new float2((blockUV.x + 1) / 16f - .001f, (blockUV.y + 1) / 16f - .001f));
                            uv.Add(new float2((blockUV.x + 1) / 16f - .001f, blockUV.y / 16f + .001f));

                            vCount += 4;
                            fCount++;
                        }
                        
                        if (!Opaque(current, left))
                        {
                            vertices.Add(blockPos + new float3(0, 0, 1));
                            vertices.Add(blockPos + new float3(0, 1, 1));
                            vertices.Add(blockPos + new float3(0, 1, 0));
                            vertices.Add(blockPos + new float3(0, 0, 0));

                            normals.Add(new float3(-1, 0, 0));
                            normals.Add(new float3(-1, 0, 0));
                            normals.Add(new float3(-1, 0, 0));
                            normals.Add(new float3(-1, 0, 0));
                            
                            blockUV = atlasMap[(((int) current) * 3) + 1];
                            uv.Add(new float2(blockUV.x / 16f + .001f, blockUV.y / 16f + .001f));
                            uv.Add(new float2(blockUV.x / 16f + .001f, (blockUV.y + 1) / 16f - .001f));
                            uv.Add(new float2((blockUV.x + 1) / 16f - .001f, (blockUV.y + 1) / 16f - .001f));
                            uv.Add(new float2((blockUV.x + 1) / 16f - .001f, blockUV.y / 16f + .001f));

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
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool Opaque(Block current, Block target)
            {
                if (target == Block.Air || target == Block.Water ||
                    target == Block.Leaves && current != Block.Leaves)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        
        [BurstCompile(FloatPrecision.Low, FloatMode.Fast, CompileSynchronously = true)]
        private struct WriteToMeshJob : IJob
        {
            [ReadOnly] public NativeList<float3> vertices;
            [ReadOnly] public NativeList<float3> normals;
            [ReadOnly] public NativeList<int>    triangles;
            [ReadOnly] public NativeList<float2> uv;

            public Mesh.MeshData outputMeshData;
            
            public unsafe void Execute()
            {
                var outputVerts   = outputMeshData.GetVertexData<float3>();
                var outputNormals = outputMeshData.GetVertexData<float3>(1);
                var outputUVs     = outputMeshData.GetVertexData<float2>(stream: 2);
                
                var vCount = vertices.Length;
                var tCount = triangles.Length;

                outputVerts.CopyFrom(vertices);
                outputNormals.CopyFrom(normals);
                outputUVs.CopyFrom(uv);
                
                if (outputMeshData.indexFormat == IndexFormat.UInt16)
                {
                    var outputTriangles = outputMeshData.GetIndexData<ushort>();
                    for (var i = 0; i < tCount; i++)
                        outputTriangles[i] = (ushort) triangles[i];
                }
                else
                {
                    var outputTriangles = outputMeshData.GetIndexData<int>();
                    outputTriangles.CopyFrom(triangles);
                }
            }
        }
    }
}