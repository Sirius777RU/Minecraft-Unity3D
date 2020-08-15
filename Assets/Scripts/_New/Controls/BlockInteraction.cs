using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityVoxelCommunityProject.Terrain;
using UnityVoxelCommunityProject.Utility;

namespace UnityVoxelCommunityProject.General.Controls
{
    public class BlockInteraction : Singleton<BlockInteraction>
    {
        public Block currentSetBlock = Block.Dirt ;
        
        public LayerMask collideWith;
        public Transform PlacementPreview;
        public float maxDistance = 8;
        public float deepenRaycastPoint = 0.2f;
        
        [Space(10), Header("Fast Mode")]
        public float waitAfterButtonDown = 0.3f;
        public float actionCooldown = 0.1f;
        
        private Transform player;
        private Transform mainCamera;
        private Transform tf;

        private float currentCooldownTime = 0;
        private float currentWaitTime = 0;
        private int height;
    
        public void Initialize()
        {
            tf = GetComponent<Transform>();
            player = PlayerMovement.Instance.tf;
            mainCamera = MouseLook.Instance.tf;
            height = SettingsHolder.Instance.proceduralGeneration.chunkHeight;
        }

        private void Update()
        {
            currentCooldownTime += Time.deltaTime;
            
            bool leftClick  = Input.GetMouseButtonDown(0);
            bool rightClick = Input.GetMouseButtonDown(1);

            bool leftClickHold  = Input.GetMouseButton(0);
            bool rightClickHold = Input.GetMouseButton(1);

            if (leftClickHold || rightClickHold)
            {
                currentWaitTime += Time.deltaTime;

                if (currentWaitTime > waitAfterButtonDown)
                {
                    leftClick = leftClickHold;
                    rightClick = rightClickHold;
                }
            }
            else
            {
                currentWaitTime = 0;
            }
            
            RaycastHit hitInfo;
            if (Physics.Raycast(mainCamera.position, mainCamera.forward, out hitInfo, maxDistance, collideWith))
            {
                //Move a little inside the block
                Vector3 selectionPoint = hitInfo.point + mainCamera.forward * deepenRaycastPoint; 
                
                tf.position = new Vector3(Mathf.FloorToInt(selectionPoint.x), 
                                          Mathf.FloorToInt(selectionPoint.y), 
                                          Mathf.FloorToInt(selectionPoint.z));
                
                Vector3 blockPoint;
                int3    blockPosition;
                Block   currentBlock;

                if(currentCooldownTime >= actionCooldown &&
                   (leftClick || rightClick))
                {
                    if (leftClick)
                    {
                        blockPoint = hitInfo.point + mainCamera.forward * deepenRaycastPoint;
                        blockPosition = new int3(Mathf.FloorToInt(blockPoint.x + 1),
                                                 Mathf.FloorToInt(blockPoint.y + 1),
                                                 Mathf.FloorToInt(blockPoint.z + 1));
                        
                        currentBlock = ChunkManager.Instance.GetBlockAtPosition(blockPosition);
                        if (currentBlock != Block.Air && currentBlock != Block.Water && currentBlock != Block.Core)
                        {
                            currentCooldownTime = 0;
                            ChunkManager.Instance.SetBlockAtPosition(blockPosition, Block.Air);
                        }
                    }
                    else if (rightClick)
                    {
                        blockPoint = hitInfo.point - mainCamera.forward * deepenRaycastPoint;
                        blockPosition = new int3(Mathf.FloorToInt(blockPoint.x + 1),
                                                 Mathf.FloorToInt(blockPoint.y + 1),
                                                 Mathf.FloorToInt(blockPoint.z + 1));
                        
                        
                        if(CheckIfPlaceable(blockPosition))
                        {
                            currentCooldownTime = 0;
                            ChunkManager.Instance.SetBlockAtPosition(blockPosition, currentSetBlock);
                        }
                    }
                }
                
                blockPoint = hitInfo.point - mainCamera.forward * deepenRaycastPoint;
                blockPoint = (new Vector3(Mathf.FloorToInt(blockPoint.x),
                                          Mathf.FloorToInt(blockPoint.y),
                                          Mathf.FloorToInt(blockPoint.z)) + Vector3.one / 2);

                if (Vector3.Distance(PlacementPreview.position, blockPoint) <= 1)
                {
                    Debug.DrawLine(PlacementPreview.position, blockPoint, Color.green);
                    PlacementPreview.LookAt(blockPoint);
                    PlacementPreview.Rotate(new Vector3(0, 180, 0));
                }
            }
            else
            {
                tf.position = Vector3.down * 1000;
            }
        }

        private bool CheckIfPlaceable(int3 position)
        {
            var currentBlock = ChunkManager.Instance.GetBlockAtPosition(position);
            if(currentBlock != Block.Air && currentBlock != Block.Water)
                return false;

            currentBlock = ChunkManager.Instance.GetBlockAtPosition(position + new int3(1, 0, 0));
            if(currentBlock != Block.Air && currentBlock != Block.Water)
                return true;
            
            currentBlock = ChunkManager.Instance.GetBlockAtPosition(position + new int3(-1, 0, 0));
            if(currentBlock != Block.Air && currentBlock != Block.Water)
                return true;
                
            currentBlock = ChunkManager.Instance.GetBlockAtPosition(position + new int3(0, 0, 1));
            if(currentBlock != Block.Air && currentBlock != Block.Water)
                return true;
            
            currentBlock = ChunkManager.Instance.GetBlockAtPosition(position + new int3(0, 0, -1));
            if(currentBlock != Block.Air && currentBlock != Block.Water)
                return true;

            if (position.y < height)
            {
                currentBlock = ChunkManager.Instance.GetBlockAtPosition(position + new int3(0, 1, 0));
                if(currentBlock != Block.Air && currentBlock != Block.Water)
                    return true;
            }

            if (position.y > 0)
            {
                currentBlock = ChunkManager.Instance.GetBlockAtPosition(position + new int3(0, -1, 0));
                if(currentBlock != Block.Air && currentBlock != Block.Water)
                    return true;
            }


            return false;
        }

    }
}
