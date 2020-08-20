using System;
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
            //stepsCount = 10000;
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
                customLightPoint.x--;
                moved = true;
            }
            
            if (Input.GetKeyDown(KeyCode.D))
            {
                customLightPoint.x++;
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
            
            var lightPoints = new NativeQueue<int3>(Allocator.TempJob);
            var lightPower = new NativeQueue<byte>(Allocator.TempJob);
            var arrows = new NativeList<int3>(Allocator.TempJob);
            var arrowPositions = new NativeList<int3>(Allocator.TempJob);
            var dots = new NativeList<int3>(Allocator.TempJob);
            
            lightPoints.Enqueue(new int3(7 + 8, 57, (7 + 8) - 12));
            lightPower.Enqueue(80);
            
            lightPoints.Enqueue(new int3(7 + 8, 57, (7 + 8) + 4));
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

                blocksCurrent = world.chunks[position].blocks,
                blocksFront   = world.chunks[position + new int2(0, 1)].blocks,
                blocksBack    = world.chunks[position + new int2(0, -1)].blocks,
                blocksLeft    = world.chunks[position + new int2(-1, 0)].blocks,
                blocksRight   = world.chunks[position + new int2(1, 0)].blocks,
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

                created.transform.position = new Vector3(arrowPosition.x + 7.5f, arrowPosition.y - 0.5f, arrowPosition.z + 7.5f);
                created.transform.LookAt(new Vector3(arrowTarget.x + 7.5f, arrowTarget.y - 0.5f, arrowTarget.z + 7.5f));
                
                arrowsGO.Add(created);
            }

            length = dots.Length;
            for (int i = 0; i < length; i++)
            {
                var dotPosition = dots[i];
                var created = Instantiate(dotPrefab, transform) as GameObject;
                
                created.transform.position = new Vector3(dotPosition.x + 7.5f, dotPosition.y - 1, dotPosition.z + 7.5f);
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

            [ReadOnly] public NativeArray<Block> blocksCurrent, 
                                                 blocksLeft, blocksRight, blocksFront, blocksBack;

            public int width, height, widthSqr;
            public int maximumStepCount;

            private int counter;
            private int currentSteps;
            private int3 direction;
            private int minWidth;
            private int lWidth;
            
            const byte lightDegradation = 10;
            
            private int3 localPosition;
            private byte localIntensity;

            private bool notCurrent;

            public void Execute()
            {
                currentSteps = 0;
                lWidth = width * 2;
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

                    //var index = GetIndex(position);
                    if(Opaque(position))
                        continue;

                    var lIndex = GetIndex(position, true);
                    currentLighting[lIndex] = intensity;

                    #region MoveAround
                    direction = new int3(-1, 0, 0);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.x > 0 && localIntensity > lightDegradation)
                    {
                        //if (currentSteps >= stepsCount)
                        //    return;
                        
                        if (!MoveAndSpread())
                            break;
                    }

                    direction      = new int3(1, 0, 0);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.x < lWidth - 1 && localIntensity > lightDegradation)
                    {
                        //if (currentSteps >= stepsCount)
                        //    return;
                        
                        if (!MoveAndSpread())
                            break;
                    }
                    
                    direction      = new int3(0, 0, -1);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.z > 0 && localIntensity > lightDegradation)
                    {
                        //if (currentSteps >= stepsCount)
                        //    return;
                        
                        if (!MoveAndSpread())
                            break;
                    }
                    
                    direction      = new int3(0, 0, 1);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.z < lWidth - 1 && localIntensity > lightDegradation)
                    {
                        //if (currentSteps >= stepsCount)
                        //    return;
                        
                        if (!MoveAndSpread())
                            break;
                    }
                    
                    direction      = new int3(0, -1, 0);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.y > 0 && localIntensity > lightDegradation)
                    {
                        //if (currentSteps >= stepsCount)
                        //    return;
                        
                        if (!MoveAndSpread())
                            break;
                    }
                    
                    direction      = new int3(0, 1, 0);
                    localPosition  = position + direction;
                    localIntensity = intensity;
                    while (localPosition.y < height - 1 && localIntensity > lightDegradation)
                    {
                        //if (currentSteps >= stepsCount)
                        //    return;
                        
                        if (!MoveAndSpread())
                            break;
                    }


                    #endregion
                    
                    //if (currentSteps >= stepsCount)
                    //    return;
                }
                
                
                //Debug.Log(currentSteps);
                //Debug.Log(calls);
                Debug.Log($"Steps {currentSteps} and DequeueCalls {calls}");
            }

            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool MoveAndSpread()
            {
                //int index = GetIndex(localPosition);
                //notCurrent = CheckIfCurrent(localPosition);
                
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

                //var index = GetIndex(spreadDirection);
                var lIndex = GetIndex(spreadDirection, true);
                var lightAtDirection = currentLighting[lIndex];
                var spreadLight = localIntensity;
                spreadLight -= lightDegradation;
                
                if (lightAtDirection != 5 &&  
                    !Opaque(localPosition) && lightAtDirection < spreadLight)
                {
                    lightPoints.Enqueue(spreadDirection);
                    lightPower.Enqueue(spreadLight);
                    counter++;

                    currentLighting[lIndex] = 5;
                    
                    //arrows.Add(spreadDirection);
                    //arrowPositions.Add(localPosition);
                }
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool Opaque(int3 position)
            {
                Block block = Block.Core;
                if(position.z < minWidth)
                {
                    position.z += width;
                    block = blocksBack[GetIndex(position)];
                }
                else if (position.z > (width + minWidth) - 1)
                {
                    position.z -= width;
                    //Debug.Log(position);
                    block = blocksFront[GetIndex(position)];
                }
                else
                {
                    block = blocksCurrent[GetIndex(position)];
                }

                if (block != Block.Air)
                {
                    return true;
                }

                return false;
            }

            private bool CheckIfCurrent(int3 position)
            {
                if (position.z < minWidth)
                {
                    return false;
                }
                
                return true;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int GetIndex(int3 position, bool forLight = false)
            {
                if (forLight)
                {
                    //Debug.Log(position);
                    //position.z = position.z + lw
                    return (lWidth * height * position.z) + (lWidth * position.y) + position.x;
                    //(32 * 64 * -1) + (32 * 57) + 8
                    //
                }
                else
                {
                    //Debug.Log(position);
                    return (width * height * (position.z - minWidth)) + (width * position.y) + (position.x - minWidth);
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