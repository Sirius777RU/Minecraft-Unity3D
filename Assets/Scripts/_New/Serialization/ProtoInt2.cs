using System.Collections;
using System.Collections.Generic;
using ProtoBuf;
using Unity.Mathematics;
using UnityEngine;

namespace UnityVoxelCommunityProject.Serialization
{
    [ProtoContract]
    public struct  ProtoInt2
    {
        [ProtoMember(1)] int x; 
        [ProtoMember(2)] int y;
        
        public ProtoInt2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static implicit operator int2(ProtoInt2 p)
        {
            return new int2(p.x, p.y);
        }

        public static implicit operator ProtoInt2(int2 p)
        {
            return new ProtoInt2(p.x, p.y);
        }
    }
}

