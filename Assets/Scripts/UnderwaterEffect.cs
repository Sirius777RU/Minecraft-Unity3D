﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityVoxelCommunityProject.Legacy;

public class UnderwaterEffect : MonoBehaviour
{
    public static bool underwater = false;
    public CurrentlyInBlock currentlyInBlock;
    public PostProcessVolume underwaterEffects; 
    
    private void Start()
    {
        
    }

    private void Update()
    {
        if (currentlyInBlock.inBlock == BlockType.Water)
        {
            underwater = true;
            underwaterEffects.weight = 1;
        }
        else
        {
            underwater = false;
            underwaterEffects.weight = 0;
        }
    }
}
