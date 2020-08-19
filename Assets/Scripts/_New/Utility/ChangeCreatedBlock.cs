using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityVoxelCommunityProject.General.Controls;
using UnityVoxelCommunityProject.Terrain;

public class ChangeCreatedBlock : MonoBehaviour
{
    public Block startWith = Block.Dirt;

    private int value = 0;
    private int maxValue = 0;

    private bool moveUp = false;
    
    private void Start()
    {
        value = (int) startWith;

        maxValue = Enum.GetNames(typeof(Block)).Length;
        Local();
    }

    private void Update()
    {
        var scroll = Input.GetAxis("Mouse ScrollWheel") * 10;
        if (scroll > 0.1f)
        {
            moveUp = true;
            value++;
            Local();
        }

        if (scroll < -0.1f)
        {
            moveUp = false;
            value--;
            Local();
        }
    }

    private void Local()
    {
        Block createdBlock = Block.Air;

        for (int i = 0; i < maxValue*2; i++)
        {
            if (value < 0)
                value = maxValue-1;

            if (value >= maxValue)
                value = 0;
            
            createdBlock = (Block) (uint) value;

            if (createdBlock == Block.Air || createdBlock == Block.Core || createdBlock == Block.Water)
            {
                if (moveUp)
                    value++;
                else
                    value--;
                
                continue;
            }
            
            break;
        }
        
        BlockInteraction.Instance.currentSetBlock = createdBlock;
    }
}
