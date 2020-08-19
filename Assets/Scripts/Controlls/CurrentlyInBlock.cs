﻿using System;
using UnityEngine;
using UnityEngine.UI;
using UnityVoxelCommunityProject.Legacy;

public class CurrentlyInBlock : MonoBehaviour
{
        private Transform tf;
        public Text text;

        public Vector3 offset = Vector3.zero;
        public BlockType inBlock;
        
        private void Start()
        {
                tf = GetComponent<Transform>();
        }

        private void FixedUpdate()
        {
                var position = tf.position + (Vector3.zero);
                
                //get the terrain chunk (can't just use collider)
                int chunkPosX = Mathf.FloorToInt((position.x + offset.x) / 16f) * 16;
                int chunkPosZ = Mathf.FloorToInt((position.z + offset.z) / 16f) * 16;

                ChunkPos cp = new ChunkPos(chunkPosX, chunkPosZ);
                
                int bix = Mathf.FloorToInt(position.x + offset.x) - chunkPosX;
                int biy = Mathf.FloorToInt(position.y + offset.y);
                int biz = Mathf.FloorToInt(position.z + offset.z) - chunkPosZ;
                
                if(biy >= SettingsHolder.Instance.currentGenerationSettings.chunkHeight || biy < 0)
                        return;

                inBlock = TerrainGenerator.getBlock(cp, bix, biy, biz);

                if (text != null)
                {
                        text.text = "" + inBlock;
                }

                /*if (Input.GetKeyDown(KeyCode.LeftControl))
                {
                        TerrainGenerator.updateChunk(cp, (blocks, updateBlock) =>
                        {
                                updateBlock(bix+1, biy, biz+1, BlockType.Dirt);
                        });   
                }*/
        }
}