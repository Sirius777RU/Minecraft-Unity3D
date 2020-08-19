﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.MemoryProfiler.Editor.NativeArrayExtensions;
using UnityEngine;
using UnityVoxelCommunityProject.Utility;

namespace UnityVoxelCommunityProject.Terrain
{
    public class TerrainLightingGeneration : Singleton<TerrainLightingGeneration>
    {
        public GameObject arrowPrefab, dotPrefab;
        public float minimumLightValue = 0.1f;
        public static int stepsCount = 0;

        private List<GameObject> arrowsGO = new List<GameObject>();
        private List<GameObject> dotGO = new List<GameObject>();

        private int3 customLightPoint = new int3(7+8, 57, 7+8);
        
        private void Start()
        {
            Shader.SetGlobalFloat("_MinimumLightIntensity", minimumLightValue);
            stepsCount = 1/*000*/;
        }

        private void Update()
        {
            return;
            
            bool moved = false;
            if (Input.GetKeyDown(KeyCode.W))
            {
                customLightPoint.z++;
                moved = true;
            }
            
            if (Input.GetKeyDown(KeyCode.S))
            {
                customLightPoint.z--;
                moved = true;
            }
            
            if (Input.GetKeyDown(KeyCode.A))
            {
                customLightPoint.x++;
                moved = true;
            }
            
            if (Input.GetKeyDown(KeyCode.D))
            {
                customLightPoint.x--;
                moved = true;
            }
            
            if (Input.GetKeyDown(KeyCode.Q))
            {
                customLightPoint.y++;
                moved = true;
            }
            
            if (Input.GetKeyDown(KeyCode.E))
            {
                customLightPoint.y--;
                moved = true;
            }
            
            if(moved)
                ChunkManager.Instance.UpdateChunks();
        }

        private void OnApplicationQuit()
        {
            stepsCount = 0;
        }

        public void RequestLightingGeneration(int2 position, NativeArray<byte> lighting)
        {
            foreach (var arrowInList in arrowsGO)
            {
                Destroy(arrowInList);
            }
            arrowsGO.Clear();

            foreach (var dot in dotGO)
            {
                Destroy(dot);
            }
            dotGO.Clear();
            
            
            int width    = SettingsHolder.Instance.proceduralGeneration.chunkWidth;
            int height   = SettingsHolder.Instance.proceduralGeneration.chunkHeight;
            
            var world = ChunkManager.Instance.dataWorld;
            var chunkCurrent = world.chunks[position];
            /*var chunkFront   = world.chunks[position + new int2(0, 1)];
            var chunkBack    = world.chunks[position + new int2(0, -1)];
            var chunkLeft    = world.chunks[position + new int2(-1, 0)];
            var chunkRight   = world.chunks[position + new int2(1, 0)];*/
            
            var lightPoints = new NativeQueue<int3>(Allocator.TempJob);
            var lightPower = new NativeQueue<byte>(Allocator.TempJob);
            var arrows = new NativeList<int3>(Allocator.TempJob);
            var arrowPositions = new NativeList<int3>(Allocator.TempJob);
            var dots = new NativeList<int3>(Allocator.TempJob);
            
            lightPoints.Enqueue(new int3(7 + 8, 57, 7 + 8));
            lightPower.Enqueue(80);
            
            var handle = new LightingJob()
            {
                customPoint = customLightPoint,
                currentLighting = lighting,
                
                lightPoints = lightPoints,
                lightPower = lightPower,
                
                arrows = arrows,
                arrowPositions = arrowPositions,
                dots = dots,
                
                width = width,
                height = height,
                widthSqr = width * width,
                
                maximumStepCount = stepsCount,

                blocksCurrent = chunkCurrent.blocks/*,
                blocksFront   = chunkFront.blocks,
                blocksBack    = chunkBack.blocks,
                blocksLeft    = chunkLeft.blocks,
                blocksRight   = chunkRight.blocks,*/
            }.Schedule();
            
            lightPoints.Dispose(handle);
            lightPower.Dispose(handle);
            
            handle.Complete();

            //Debug.Log(stepsCount);
            stepsCount++;
            //lightingJob.Execute();

            int length = arrows.Length;
            for (int i = 0; i < length; i++)
            {
                var arrowPosition = arrows[i];
                var arrowTarget = arrowPositions[i];
                var created = Instantiate(arrowPrefab, transform) as GameObject;

                created.transform.position = new Vector3(arrowPosition.x + 15.5f, arrowPosition.y - 1, arrowPosition.z + 15.5f);
                created.transform.LookAt(new Vector3(arrowTarget.x + 15.5f, arrowTarget.y - 1, arrowTarget.z + 15.5f));
                
                arrowsGO.Add(created);
            }

            length = dots.Length;
            for (int i = 0; i < length; i++)
            {
                var dotPosition = dots[i];
                var created = Instantiate(dotPrefab, transform) as GameObject;
                
                created.transform.position = new Vector3(dotPosition.x + 15.5f, dotPosition.y - 1, dotPosition.z + 15.5f);
                if(i == length - 1) created.transform.localScale = new Vector3(4, 1, 4);
                
                dotGO.Add(created);
            }
            
            arrows.Dispose();
            arrowPositions.Dispose();
            dots.Dispose();
        }
        
        //TODO find highest point and lit sunlight & bake it somehow as separate chanel.
        //Treat each light beyond current chunk.
        
        [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        private struct LightingJob : IJob
        {
            public NativeArray<byte> currentLighting;
            public NativeQueue<int3> lightPoints;
            public NativeQueue<byte> lightPower;

            public NativeList<int3> arrows;
            public NativeList<int3> arrowPositions;
            public NativeList<int3> dots;

            public int3 customPoint;

            [ReadOnly] public NativeArray<Block> blocksCurrent;//, 
                                                 //blocksLeft, blocksRight, blocksFront, blocksBack;

            public int width, height, widthSqr;
            public int maximumStepCount;

            private int counter;
            private int currentSteps;
            private int3 direction;
            private int minWidth;
            private int lWidth;
            
            public void Execute()
            {
                currentSteps = 0;
                lWidth = width * width;
                minWidth = (width / 2);
                
                currentLighting.MemClear();
                currentSteps++;

                counter = lightPoints.Count;
                int calls = 0;

                //currentLighting[GetIndex(customPoint, true)] = 80;
                //return;

                while (counter > 0) //As long as we got light data to compute
                {
                    var position  = lightPoints.Dequeue();
                    var intensity = lightPower.Dequeue();
                    counter--;
                    calls++;

                    localPosition  = position;
                    localIntensity = intensity;

                    var index = GetIndex(position);
                    if(Opaque(GetBlock(index)))
                        continue;

                    var lIndex = GetIndex(position, true);
                    currentLighting[lIndex] = intensity;

                    #region NoveAround
                    direction = new int3(-1, 0, 0);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.x > minWidth && localIntensity > lightDegradation)
                    {
                        if (currentSteps >= stepsCount)
                            return;
                        
                        if (!MoveAndSpread())
                            break;
                    }

                    direction      = new int3(1, 0, 0);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.x < lWidth - 1 && localIntensity > lightDegradation)
                    {
                        if (currentSteps >= stepsCount)
                            return;
                        
                        if (!MoveAndSpread())
                            break;
                    }
                    
                    direction      = new int3(0, 0, -1);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.z > minWidth && localIntensity > lightDegradation)
                    {
                        if (currentSteps >= stepsCount)
                            return;
                        
                        if (!MoveAndSpread())
                            break;
                    }
                    
                    direction      = new int3(0, 0, 1);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.z < lWidth - 1 && localIntensity > lightDegradation)
                    {
                        if (currentSteps >= stepsCount)
                            return;
                        
                        if (!MoveAndSpread())
                            break;
                    }
                    
                    direction      = new int3(0, -1, 0);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.y > 0 && localIntensity > lightDegradation)
                    {
                        if (currentSteps >= stepsCount)
                            return;
                        
                        if (!MoveAndSpread())
                            break;
                    }
                    
                    direction      = new int3(0, 1, 0);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.y < height - 1 && localIntensity > lightDegradation)
                    {
                        if (currentSteps >= stepsCount)
                            return;
                        
                        if (!MoveAndSpread())
                            break;
                    }


                    #endregion
                    
                    //if (currentSteps >= stepsCount)
                    //    return;
                }
                
                
                //Debug.Log(currentSteps);
                //Debug.Log(calls);
                //Debug.Log($"Steps {currentSteps} and DequeueCalls {calls}");
            }

            const byte lightDegradation = 10;
            
            private int3 localPosition;
            private byte localIntensity;
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool MoveAndSpread()
            {
                int index = GetIndex(localPosition);
                var lIndex = GetIndex(localPosition, true);
                var light = currentLighting[lIndex];
                        
                localIntensity -= lightDegradation;

                if (!Opaque(GetBlock(index)) && light < localIntensity)
                {
                    currentLighting[lIndex] = localIntensity;

                    if (localIntensity >= lightDegradation)
                    {
                        if (direction.x >= 0 && localPosition.x > minWidth)
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
                        
                        currentSteps++;
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

            //I think we spawn some points totally wrong, I need to check count and exact line where it's happens 
            
            
            private void TryToSpread(int3 spreadDirection)
            {
                spreadDirection += localPosition;
                
                var index = GetIndex(spreadDirection);
                var lIndex = GetIndex(spreadDirection, true);
                var lightAtDirection = currentLighting[lIndex];
                var spreadLight = localIntensity;
                spreadLight -= lightDegradation;
                
                if (lightAtDirection != 5 &&  
                    !Opaque(GetBlock(index)) && lightAtDirection < spreadLight)
                {
                    lightPoints.Enqueue(spreadDirection);
                    lightPower.Enqueue(spreadLight);
                    counter++;

                    currentLighting[lIndex] = 5;
                }
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private Block GetBlock(int index)
            {
                return blocksCurrent[index];
                
                /*if (orientation == ChunkOrientation.current)
                {
                    return blocksCurrent[index];
                }
                else if(orientation == ChunkOrientation.back)
                {
                    return blocksBack[index];
                }*/

                return Block.Core;
            }

            private int Occlusion(int3 position, int intensity)
            {
                int index = 0;
                int occlusion = 2;

                index = GetIndex(position + new int3(-1, 0, 0));
                if (Opaque(blocksCurrent[index]))
                    intensity -= occlusion;

                index = GetIndex(position + new int3(1, 0, 0));
                if (Opaque(blocksCurrent[index]))
                    intensity -= occlusion;

                index = GetIndex(position + new int3(0, 0, 1));
                if (Opaque(blocksCurrent[index]))
                    intensity -= occlusion;
                    
                index = GetIndex(position + new int3(0, 0, -1));
                if (Opaque(blocksCurrent[index]))
                    intensity -= occlusion;
                
                index = GetIndex(position + new int3(0, 1, 0));
                if (Opaque(blocksCurrent[index]))
                    intensity -= occlusion;
                
                index = GetIndex(position + new int3(0, -1, 0));
                if (Opaque(blocksCurrent[index]))
                    intensity -= occlusion;
                
                return intensity;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool Opaque(Block block)
            {
                
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
                    return ((position.x) + (position.z) * lWidth + (position.y) * (height));
                }
                else
                {
                    return (position.x - minWidth) + (position.z - minWidth) * width + position.y * widthSqr;
                }
            }
            
            private enum ChunkOrientation : byte
            {
                current,
                front,
                back,
                left,
                right,
                
                frontLeft,
                frontRight,
                backLeft,
                backRight,
            }
            
        }
    }
}