using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityVoxelCommunityProject.Utility;

namespace UnityVoxelCommunityProject.Terrain
{
    public class TerrainLightingGeneration : Singleton<TerrainLightingGeneration>
    {
        public float minimumLightValue = 0.1f;

        private void Start()
        {
            Shader.SetGlobalFloat("_MinimumLightIntensity", minimumLightValue);
        }

        public void RequestLightingGeneration(int2 position, NativeArray<byte> lighting)
        {
            int width    = SettingsHolder.Instance.proceduralGeneration.chunkWidth;
            int height   = SettingsHolder.Instance.proceduralGeneration.chunkHeight;

            var world = ChunkManager.Instance.dataWorld;
            
            var lightPoints = new NativeQueue<int3>(Allocator.TempJob);
            var lightPower = new NativeQueue<byte>(Allocator.TempJob);

            var chunkCurrent = world.chunks[position];
            var chunkN = world.chunks[position + new int2( 0,  1)];
            var chunkS = world.chunks[position + new int2( 0, -1)];
            var chunkW = world.chunks[position + new int2(-1,  0)];
            var chunkE = world.chunks[position + new int2( 1,  0)];

            var chunkNW = world.chunks[position + new int2(-1,  1)];
            var chunkNE = world.chunks[position + new int2( 1,  1)];
            var chunkSW = world.chunks[position + new int2(-1, -1)];
            var chunkSE = world.chunks[position + new int2( 1, -1)];

            GetLightSources(lightPoints, lightPower, chunkCurrent, int2.zero);
            GetLightSources(lightPoints, lightPower, chunkN, new int2( 0, 1));
            GetLightSources(lightPoints, lightPower, chunkS, new int2( 0,-1));
            GetLightSources(lightPoints, lightPower, chunkW, new int2(-1, 0));
            GetLightSources(lightPoints, lightPower, chunkE, new int2( 1, 0));
            
            GetLightSources(lightPoints, lightPower, chunkNW, new int2(-1,  1));
            GetLightSources(lightPoints, lightPower, chunkNE, new int2( 1,  1));
            GetLightSources(lightPoints, lightPower, chunkSW, new int2(-1, -1));
            GetLightSources(lightPoints, lightPower, chunkSE, new int2( 1, -1));
            
            var handle = new LightingJob()
            {
                currentLighting = lighting,
                
                lightPoints = lightPoints,
                lightPower = lightPower,
                
                width = width,
                height = height,

                blocksCurrent = chunkCurrent.blocks,
                blocksN = chunkN.blocks,
                blocksS = chunkS.blocks,
                blocksW = chunkW.blocks,
                blocksE = chunkE.blocks,
                
                blocksNW = chunkNW.blocks,
                blocksNE = chunkNE.blocks,
                blocksSW = chunkSW.blocks,
                blocksSE = chunkSE.blocks
            }.Schedule();
            
            lightPoints.Dispose(handle);
            lightPower.Dispose(handle);
            
            handle.Complete();
        }

        private void GetLightSources(NativeQueue<int3> lightPoints, NativeQueue<byte> lightPower, DataChunk chunk, int2 direction)
        {
            var length = chunk.lightSources.Count;
            for (int i = 0; i < length; i++)
            {
                var position = chunk.lightSources[i].Item1;
                var intensity = chunk.lightSources[i].Item2;

                int3 localPosition = position;
                localPosition.x += direction.x * 16;
                localPosition.z += direction.y * 16;

                if (Mathf.Abs(8 - localPosition.x) < 16 && Mathf.Abs(8 - localPosition.z) < 16)
                {
                    lightPoints.Enqueue(localPosition + new int3(8, 0, 8));
                    lightPower.Enqueue(intensity);
                }
            }
        }
        
        //TODO find highest point and lit sunlight & bake it somehow as separate chanel so we could change it without any additional computations.
        [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        private struct LightingJob : IJob
        {
            public NativeArray<byte> currentLighting;
            public NativeQueue<int3> lightPoints;
            public NativeQueue<byte> lightPower;

            [ReadOnly] public NativeArray<Block> blocksCurrent, 
                                                 blocksW, blocksE, blocksN, blocksS,
                                                 blocksNW, blocksNE, blocksSW, blocksSE;

            public int width, height;

            private int counter;
            private int3 direction;
            private int minWidth;
            private int lWidth;
            
            const byte lightDegradation = 10;
            
            private int3 localPosition;
            private byte localIntensity;

            public unsafe void Execute()
            {
                lWidth = width * 2;
                minWidth = (width / 2);
                
                var length = currentLighting.Length;
                UnsafeUtility.MemClear(currentLighting.GetUnsafePtr(), length);

                counter = lightPoints.Count;

                while (counter > 0) 
                {
                    var position  = lightPoints.Dequeue();
                    var intensity = lightPower.Dequeue();
                    counter--;

                    localPosition  = position;
                    localIntensity = intensity;

                    if(Opaque(position))
                        continue;

                    var lIndex = GetIndex(position, true);
                    var light = currentLighting[lIndex];
                    if(light >= intensity)
                        continue;
                        
                    currentLighting[lIndex] = intensity;

                    #region MoveAround
                    direction = new int3(-1, 0, 0);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.x > 0 && localIntensity > lightDegradation)
                    {
                        if (!MoveAndSpread())
                            break;
                    }

                    direction      = new int3(1, 0, 0);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.x < lWidth - 1 && localIntensity > lightDegradation)
                    {
                        if (!MoveAndSpread())
                            break;
                    }
                    
                    direction      = new int3(0, 0, -1);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.z > 0 && localIntensity > lightDegradation)
                    {
                        if (!MoveAndSpread())
                            break;
                    }
                    
                    direction      = new int3(0, 0, 1);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.z < lWidth - 1 && localIntensity > lightDegradation)
                    {
                        if (!MoveAndSpread())
                            break;
                    }
                    
                    direction      = new int3(0, -1, 0);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.y > 0 && localIntensity > lightDegradation)
                    {
                        if (!MoveAndSpread())
                            break;
                    }
                    
                    direction      = new int3(0, 1, 0);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.y < height - 1 && localIntensity > lightDegradation)
                    {
                        if (!MoveAndSpread())
                            break;
                    }
                    #endregion
                }
            }

            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool MoveAndSpread()
            {
                var lIndex = GetIndex(localPosition, true);
                var light = currentLighting[lIndex];
                        
                localIntensity -= lightDegradation;

                if (!Opaque(localPosition) && light < localIntensity)
                {
                    currentLighting[lIndex] = localIntensity;

                    if (localIntensity >= lightDegradation)
                    {
                        if (direction.x >= 0 )
                            TryToSpread(new int3(-1, 0, 0));
                        
                        if (direction.x <= 0 && localPosition.x < lWidth - 1)
                            TryToSpread(new int3(1, 0, 0));
                        
                        if (direction.z >= 0 && localPosition.z > minWidth)
                            TryToSpread(new int3(0, 0, -1));
                        
                        if (direction.z <= 0 && localPosition.z < lWidth - 1)
                            TryToSpread(new int3(0, 0, 1));

                        if (direction.y >= 0 && localPosition.y > 0)
                            TryToSpread(new int3(0, -1, 0));
                        
                        if (direction.y <= 0 && localPosition.y < height - 1)
                            TryToSpread(new int3(0, 1, 0));
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }


                localPosition += direction;
                return true;
            }
            
            private void TryToSpread(int3 spreadDirection)
            {
                spreadDirection += localPosition;

                var lIndex = GetIndex(spreadDirection, true);
                var lightAtDirection = currentLighting[lIndex];

                var spreadLight = localIntensity;
                spreadLight -= lightDegradation;
                
                var spreadMark = localIntensity / 10;
                if (lightAtDirection < 10)
                {
                    if (spreadMark <= lightAtDirection)
                    {
                        return;
                    }
                }

                if (!Opaque(localPosition) && lightAtDirection < spreadLight)
                {
                    lightPoints.Enqueue(spreadDirection);
                    lightPower.Enqueue(spreadLight);
                    counter++;

                    currentLighting[lIndex] = (byte) spreadMark;
                }
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool Opaque(int3 position)
            {
                Block block = Block.Core;
                if (position.z > (width + minWidth) - 1 && position.x < minWidth)
                {
                    position += new int3(width, 0, -width);
                    block = blocksNW[GetIndex(position)];
                }
                else if (position.z > (width + minWidth) - 1 && position.x > (width + minWidth) - 1)
                {
                    position += new int3(-width, 0, -width);
                    block = blocksNE[GetIndex(position)];
                }
                else if (position.z < minWidth && position.x < minWidth)
                {
                    position += new int3(width, 0, width);
                    block = blocksSW[GetIndex(position)];
                }
                else if (position.z < minWidth && position.x > (width + minWidth) - 1)
                {
                    position += new int3(-width, 0, width);
                    block = blocksSE[GetIndex(position)];
                }
                else if(position.z < minWidth)
                {
                    position.z += width;
                    block = blocksS[GetIndex(position)];
                }
                else if (position.z > (width + minWidth) - 1)
                {
                    position.z -= width;
                    block = blocksN[GetIndex(position)];
                }
                else if (position.x < minWidth)
                {
                    position.x += width;
                    block =  blocksW[GetIndex(position)];
                }
                else if (position.x > (width + minWidth) - 1)
                {
                    position.x -= width;
                    block =  blocksE[GetIndex(position)];
                }
                else
                {
                    block = blocksCurrent[GetIndex(position)];
                }

                //TODO light should consider opacity rather than just if certain block is air or not.
                if (block != Block.Air)
                {
                    return true;
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int GetIndex(int3 position, bool forLight = false)
            {
                if (forLight)
                {
                    return (lWidth * height * position.z) + (lWidth * position.y) + position.x;
                }
                else
                {
                    return (width * height * (position.z - minWidth)) + (width * position.y) + (position.x - minWidth);
                }
            }
        }
    }
}