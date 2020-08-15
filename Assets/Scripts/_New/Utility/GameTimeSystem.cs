using System;
using UnityEngine;
using UnityVoxelCommunityProject.Utility;

namespace UnityVoxelCommunityProject.General
{
    public class GameTimeSystem : Singleton<GameTimeSystem>
    {
        [Range(1, 1024)] public int everyFrames = 1;  
        public DeltaTimeMode deltaTimeMode = DeltaTimeMode.deltaTime;
        public float         speed         = 1;
        public SkySphere     skySphere;

        public static float time           = 1;
        public static int   daysSinceStart = 0;

        private float dtSum = 0;
        
        private void Start()
        {
            time      = skySphere.currentTime;
        }

        private void Update()
        {
            if (PauseMenu.pause)
                return;

            float dt = GetDeltaTime(deltaTimeMode);
            dtSum += dt;
            
            if(Time.frameCount % everyFrames != 0)
                return;

            dt = dtSum;
            dtSum = 0;

            time += (speed * 0.01f) * dt;
            if (time > 1f)
            {
                time = 0;
                daysSinceStart++;
            }

            if (skySphere != null)
            {
                skySphere.currentTime = time;
            }
        }


        public static float GetDeltaTime(DeltaTimeMode deltaTimeMode)
        {
            float dt = 0;
            switch (deltaTimeMode)
            {
                case DeltaTimeMode.deltaTime:
                    dt = Time.deltaTime;

                    break;

                case DeltaTimeMode.smoothDeltaTime:
                    dt = Time.smoothDeltaTime;

                    break;

                case DeltaTimeMode.fixedDeltaTime:
                    dt = Time.fixedDeltaTime;

                    break;

                case DeltaTimeMode.unscaledDeltaTime:
                    dt = Time.unscaledDeltaTime;

                    break;
                case DeltaTimeMode.captureDeltaTime:
                    dt = Time.captureDeltaTime;

                    break;

                default:

                    throw new ArgumentOutOfRangeException();
            }

            return dt;
        }
    }
}