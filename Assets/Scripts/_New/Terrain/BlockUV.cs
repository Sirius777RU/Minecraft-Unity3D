using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityVoxelCommunityProject.Terrain
{
    [Serializable]
    public struct BlockUV
    {
        public int2 defaultTexture;
        public int2 sideTexture;
        public int2 bottomTexture;
        
        public int2 Default()
        {
            return defaultTexture;
        }

        public int2 Side()
        {
            if (sideTexture.x >= 0)
            {
                return sideTexture;
            }

            return defaultTexture;
        }

        public int2 Bottom()
        {
            if (bottomTexture.x >= 0)
            {
                return bottomTexture;
            }

            return defaultTexture;
        }
    }
}