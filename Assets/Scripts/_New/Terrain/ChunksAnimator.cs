using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityVoxelCommunityProject.Terrain
{
    public class ChunksAnimator : Singleton<ChunksAnimator>
    {
        public AnimationCurve curve;
        [Space][Range(1, 100)] public float speed = 1;
        public bool use = true;
        
        private Queue<Tuple<float, Chunk>> animatedChunks = new Queue<Tuple<float, Chunk>>();

        public void Register(Chunk chunk)
        {
            if (!use)
            {
                return;
            }
            
            animatedChunks.Enqueue(new Tuple<float, Chunk>(0f, chunk));
        }

        private void LateUpdate()
        {
            float dt = (Time.deltaTime * speed);
            int length = animatedChunks.Count;
            float animationDuration = curve[curve.length - 1].time;
            
            for (int i = 0; i < length; i++)
            {
                var tuple = animatedChunks.Dequeue();
                var time = tuple.Item1 + dt;
                var chunk = tuple.Item2;

                var temp = chunk.tf.position;
                temp.y = curve.Evaluate(time);
                chunk.tf.position = temp;
                
                if(time < animationDuration)
                    animatedChunks.Enqueue(new Tuple<float, Chunk>(time, chunk));
            }
        }
    }
}