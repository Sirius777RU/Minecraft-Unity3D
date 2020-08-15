using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityVoxelCommunityProject.Utility
{
    public class TransparencySine : MonoBehaviour
    {
        public Material material;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 100);
        public float speed = 1;
        
        private int id = 0;
        private float progress = 0;
        
        private void Start()
        {
            id = Shader.PropertyToID("_Color");
        }

     
        private void Update()
        {
            //float maxTime = curve.keys[curve.length - 1].time;
            progress += (Time.unscaledDeltaTime * speed);
            
            var color = material.GetColor(id);
            color.a = curve.Evaluate(progress);
            material.SetColor(id, color);
        }
    }
}
