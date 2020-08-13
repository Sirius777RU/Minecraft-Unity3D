using UnityEngine;
using UnityVoxelCommunityProject.General.Controls;

namespace UnityVoxelCommunityProject.General
{
    public class AntiFloatPointOrigin : Singleton<AntiFloatPointOrigin>
    {
        [Range(16, 1024)]
        public float limit = 32;
        public Vector3 offset = Vector3.zero; 
    
        private Transform tf;
        
        private AntiFloatPointObject[] antiFloatObjects;
        private Transform[] antiFloatTransforms;
        
        private Vector3 compensation = Vector3.zero;
        
        private void Start()
        {
            tf = GetComponent<Transform>();
            antiFloatObjects = FindObjectsOfType<AntiFloatPointObject>(false);

            offset.x = Mathf.Floor(tf.position.x/limit)*limit;
            offset.z = Mathf.Floor(tf.position.z/limit)*limit;

            var temp    = tf.position - offset;
            temp.y      = tf.position.y;
            tf.position = temp;

            int length = antiFloatObjects.Length;
            antiFloatTransforms = new Transform[length];
            
            for (int i = 0; i < length; i++)
            {
                antiFloatTransforms[i] = antiFloatObjects[i].GetComponent<Transform>();

                if (antiFloatObjects[i].intentionallyPlaced)
                {
                    antiFloatTransforms[i].position = antiFloatTransforms[i].position - offset;
                }
            }
        }
        
        private void LateUpdate()
        {
            if(Mathf.Abs(tf.position.x) >= limit || Mathf.Abs(tf.position.z) >= limit)
                Local();
        }
        
        private void Local()
        {
            Vector3 temp = tf.position; 
        
            if (tf.position.x >= limit)
            {
                compensation.x -= limit*2;
            }
            else if (tf.position.x <= -limit)
            {
                compensation.x += limit*2;
            }

            if (tf.position.z >= limit)
            {
                compensation.z -= limit*2;
            }
            else if (tf.position.z <= -limit)
            {
                compensation.z += limit*2;
            }

        
            tf.position = temp + (compensation);
            offset += (-compensation);
        
            int length = antiFloatObjects.Length;
            for (int i = 0; i < length; i++)
            {
                antiFloatTransforms[i].position = antiFloatTransforms[i].position + (compensation);
            }
        
            compensation = Vector3.zero;
        }
    }
}