using System;
using UnityEngine;

namespace UnityCommunityVoxelProject.General.Controls
{
    [SelectionBase][RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : Singleton<PlayerMovement>
    {
        public PlayerMovementSettings settings;

        [HideInInspector] public Transform tf;
        [HideInInspector] public CharacterController controller;

        private RaycastHit hitInfo;
        
        private float finalRayLength;

        private bool isRunning;
        private bool isGrounded;
        private bool previouslyGrounded;
        private bool hitWall;

        private Vector2 inputVector, 
                        inputVectorSmooth;
        
        private Vector3 finalMoveDir;
        private Vector3 smoothFinalMoveDir;
        private Vector3 finalMoveVector;
        
        private float currentSpeed;
        private float smoothCurrentSpeed;
        private float finalSmoothCurrentSpeed;
        private float walkRunSpeedDifference;

        private float initHeight;
        private Vector3 initCenter;
        private bool  duringRunAnimation;
        
        private float inAirTimer;
        private float dt;
        
        private void Start()
        {
            tf = GetComponent<Transform>();
            controller = GetComponent<CharacterController>();
            
            Initialize();
        }

        private void Update()
        {
            dt = Time.deltaTime;
            CheckIfGrounded();
            
            SmoothInput();
            SmoothSpeed();
            SmoothDir();
            
            CalculateMovementDirection();
            CalculateSpeed();
            CalculateFinalMovement();
            
            ApplyGravity();
            ApplyMovement();

            previouslyGrounded = isGrounded;
        }

        private void Initialize()
        {
            finalRayLength = settings.rayLength + controller.center.y;
        }


        private void CheckIfGrounded()
        {
            Vector3 origin = tf.position + controller.center;

            bool hitGround = Physics.SphereCast(origin, settings.raySphereRadius,Vector3.down, out hitInfo,finalRayLength, settings.groundLayer);
            Debug.DrawRay(origin,Vector3.down * (finalRayLength), Color.red);

            isGrounded = hitGround;
        }

        #region Smoothing
        private void SmoothInput()
        {
            inputVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized;
            isRunning = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            
            inputVectorSmooth = Vector2.Lerp(inputVectorSmooth,inputVector,dt * settings.smoothInput);
        }

        private void SmoothSpeed()
        {
            smoothCurrentSpeed = Mathf.Lerp(smoothCurrentSpeed, currentSpeed, dt * settings.smoothVelocity);

            if(isRunning)
            {
                walkRunSpeedDifference = (settings.walkSpeed * settings.runModifier) - settings.walkSpeed;
                
               float walkRunPercent = Mathf.InverseLerp(settings.walkSpeed,settings.walkSpeed * settings.runModifier, smoothCurrentSpeed);
               finalSmoothCurrentSpeed = settings.runTransition.Evaluate(walkRunPercent) * walkRunSpeedDifference + settings.walkSpeed;
            }
            else
            {
                finalSmoothCurrentSpeed = smoothCurrentSpeed;
            }
        }
        
        protected virtual void SmoothDir()
        {
            smoothFinalMoveDir = Vector3.Lerp(smoothFinalMoveDir, finalMoveDir, dt * settings.smoothFinalDirection);
        }
        #endregion
        
        private void CalculateMovementDirection()
        {

            Vector3 _vDir = tf.forward * inputVectorSmooth.y;
            Vector3 _hDir = tf.right   * inputVectorSmooth.x;

            Vector3 _desiredDir = _vDir + _hDir;
            Vector3 _flattenDir = FlattenVectorOnSlopes(_desiredDir);

            finalMoveDir = _flattenDir;
        }
        
        private Vector3 FlattenVectorOnSlopes(Vector3 vectorToFlat)
        {
            if(isGrounded)
                vectorToFlat = Vector3.ProjectOnPlane(vectorToFlat, hitInfo.normal);
                    
            return vectorToFlat;
        }
        
        private void CalculateSpeed()
        {
            currentSpeed = isRunning ? settings.walkSpeed * settings.runModifier : settings.walkSpeed;
            currentSpeed = inputVector.y == -1 ? currentSpeed * settings.backModifier : currentSpeed;
            currentSpeed = inputVector.x != 0 && inputVector.y ==  0 ? currentSpeed * settings.sideModifier :  currentSpeed;
        }

        private void CalculateFinalMovement()
        {
            Vector3 finalVector = smoothFinalMoveDir * finalSmoothCurrentSpeed;
            
            finalMoveVector.x = finalVector.x;
            finalMoveVector.z = finalVector.z;

            if(controller.isGrounded)
                finalMoveVector.y += finalVector.y;
        }
        
        private void ApplyGravity()
        {
            if(controller.isGrounded)
            {
                inAirTimer        = 0f;
                finalMoveVector.y = -settings.stickToGroundForce;

                HandleJump();
            }
            else
            {
                inAirTimer      += dt;
                finalMoveVector += Physics.gravity * settings.gravityMultiplier * dt;
            }
        }
        
        private void HandleJump()
        {
            if(Input.GetButton("Jump"))
            {
                finalMoveVector.y = settings.jumpSpeed;
                    
                previouslyGrounded = true;
                isGrounded         = false;
            }
        }

        protected virtual void ApplyMovement()
        {
            controller.Move(finalMoveVector * dt);
        }
    }
}