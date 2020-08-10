using System.Collections;
using UnityEngine;

namespace UnityVoxelCommunityProject.General
{
    public static class WaitFor
    {
        public static IEnumerator Frames(int frameCount)
        {
            while (frameCount > 0)
            {
                frameCount--;
                yield return null;
            }
        }
    }
}