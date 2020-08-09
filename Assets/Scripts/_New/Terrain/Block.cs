using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityVoxelCommunityProject.Terrain
{
    //Byte is fastest option, but limited to just 256 types.
    //Change to something with bigger range(ushort, uint) if needed.
    public enum Block : byte
    {
        Air, 
        Dirt, 
        Grass, 
        Stone, 
        Trunk, 
        Leaves, 
        Water, 
        Sand,
        Luminore,
        Core 
    }
}