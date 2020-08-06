using System;
using Unity.Mathematics;
using UnityEngine;

namespace UnityCommunityVoxelProject.General.Controls
{
    public class MouseLook : Singleton<MouseLook>
    {
        public bool smoothRotation = false;
        public float damping = 20;
        
        [Space(10)]
        public float2 mouseSensitivity = new float2(400, 100);
        [Space(10)]
        public float minimumYRotation = -70;
        public float maximumYRotation = 70;

        [Space(10)]
        public bool2 invertXY = new bool2(false, false);
        
        private Transform tf;
        private float desiredX, desiredY;
        private float currentX, currentY;
        
        private void Start()
        {
            tf = GetComponent<Transform>();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float mouseX = (Input.GetAxis("Mouse X") * mouseSensitivity.x) * dt;
            float mouseY = (Input.GetAxis("Mouse Y") * mouseSensitivity.y) * dt;

            desiredX = desiredX + (invertXY.x ? -mouseX : mouseX);
            desiredY = desiredY - (invertXY.x ? -mouseY : mouseY);

            desiredY = Mathf.Clamp(desiredY, minimumYRotation, maximumYRotation);
            
            if (smoothRotation)
            {
                currentX = Mathf.Lerp(currentX, desiredX, dt * damping);
                currentY = Mathf.Lerp(currentY, desiredY, dt * damping);
            }
            else
            {
                currentX = desiredX;
                currentY = desiredY;
            }

            if (PlayerMovement.Instance.tf)
            {
                PlayerMovement.Instance.tf.eulerAngles = Vector3.up * currentX;
                tf.localRotation = Quaternion.Euler(currentY, 0, 0);
            }
            else
            {
                tf.localRotation = Quaternion.Euler(currentY, currentX, 0);
            }
            
            
        }
    }
}