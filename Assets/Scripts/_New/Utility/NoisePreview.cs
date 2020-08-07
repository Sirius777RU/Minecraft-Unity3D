using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace UnityVoxelCommunityProject.Utility
{
    public class NoisePreview : MonoBehaviour
    {
        public bool p = false;
        public  RawImage  image;
        public  float     noiseScale1      = 0.1f, noiseScale2      = 0.3f;
        public  float     noiseMultiplier1 = 1,    noiseMultiplier2 = 1;
        
        private Texture2D texture2D;

        private void Update()
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                Local();
            }
        }
        
        private void OnValidate()
        {
            Local();
        }

        private void Local()
        {
            texture2D            = new Texture2D(256, 256);
            texture2D.filterMode = FilterMode.Point;

            Color[] colors       = new Color[256 * 256];
            int     colorCounter = 0;

            Color currentColor;
            float baseValue = 0.5f;
            
            
            for (int y = 0; y < 256; y++)
            for (int x = 0; x < 256; x++)
            {
                float noiseValue = 0;
                
                if(p) noiseValue += math.unlerp(-1, 1, noise.pnoise(new float2(x * noiseScale1, y * noiseScale1),  new float2(x, y)));
                else
                {
                    //noiseValue += noise.snoise(new float2(x * noiseScale1, y * noiseScale1)) * noiseMultiplier1;
                    noiseValue += noise.snoise(new float2(x * noiseScale2, y * noiseScale2)) * noiseMultiplier2;
                    noiseValue =  math.unlerp(-1, 1, noiseValue);
                }
                
                currentColor = new Color(noiseValue, noiseValue, noiseValue);
                
                colors[colorCounter] = currentColor;
                colorCounter++;
            }
                
            texture2D.SetPixels(colors);
            texture2D.Apply(false);

            image.texture = texture2D;
        }
    }
}