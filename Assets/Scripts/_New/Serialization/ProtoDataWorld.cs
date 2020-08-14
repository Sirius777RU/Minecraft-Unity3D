using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Unity.Mathematics;
using UnityVoxelCommunityProject.Serialization;

namespace UnityVoxelCommunityProject
{
    [ProtoContract]
    public class ProtoDataWorld
    {
        //TODO Think of file structure.
        [ProtoMember(1)] public Dictionary<ProtoInt2, ProtoDataChunk> chunks;
        
        public static implicit operator DataWorld(ProtoDataWorld p)
        {
            var result = new DataWorld()
            {
                chunks = new Dictionary<int2, DataChunk>()
            };

            var keys   = p.chunks.Keys.ToArray();
            var values = p.chunks.Values.ToArray();
            
            for (int i = 0; i < p.chunks.Count; i++)
            {
                result.chunks.Add(keys[i], values[i]);
            }

            return result;
        }

        public static implicit operator ProtoDataWorld(DataWorld p)
        {
            var result = new ProtoDataWorld()
            {
                chunks = new Dictionary<ProtoInt2, ProtoDataChunk>()
            };

            var keys   = p.chunks.Keys.ToArray();
            var values = p.chunks.Values.ToArray();
            
            for (int i = 0; i < p.chunks.Count; i++)
            {
                result.chunks.Add(keys[i], values[i]);
            }

            return result;
        }
    }
}