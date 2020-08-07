using UnityEngine;

namespace UnityVoxelCommunityProject.General
{
    [CreateAssetMenu(fileName = "Player Movement Settings", menuName = "Settings/Player Movement Settings", order = 0)]
    public class PlayerMovementSettings : ScriptableObject
    {
        [Space, Header("Locomotion")]
        public float walkSpeed   = 5f;
        public float jumpSpeed   = 12f;
        
        [Space(10), Header("Speed Multipliers")]
        [Range(1f, 10f)] public float runModifier = 2.5f;
        [Range(0f, 1f)] public float sideModifier = 0.85f;
        [Range(0f, 1f)] public float backModifier  = 0.6f;
        [Range(0f, 1f)] public float underwater = 0.5f;
        
        [Space(10), Header("Curves")]
        public AnimationCurve runTransition = AnimationCurve.EaseInOut(0f,0f,1f,1f);
        
        [Space, Header("Gravity")]
        public float gravityMultiplier = 5f;
        public float stickToGroundForce = 5f;
                    
        public LayerMask groundLayer     = ~0;
        
        [Range(0f, 1f)]    public float rayLength       = 0.2f;
        [Range(0.01f, 1f)] public float raySphereRadius = 0.3f;
        
        [Space, Header("Smooth")]
        [Range(1f, 100f)] public float smoothInput          = 70f;
        [Range(1f, 100f)] public float smoothVelocity       = 5f;
        [Range(1f, 100f)] public float smoothFinalDirection = 15f;
        [Range(1f, 100f)] public float smoothHeadBob        = 5f;
    }
}