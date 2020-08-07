using System;
using System.Collections.Generic;
using UnityEngine;
using UnityVoxelCommunityProject.Legacy;
using Random = UnityEngine.Random;

[SelectionBase]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 6.0f;
    public float acceleratedSpeed = 12f;
    
    public float jumpSpeed = 1.0f;
    public float gravity = -9.8f;
    public float jumpHeight = 3f;

    public float groundDistance = .4f;
    public bool isMainController = false;
    public bool isChase = false;

    public float jumpCooldown = 0.3f;
    [Space(10)] 
    public CurrentlyInBlock headInBlock;
    public CurrentlyInBlock bodyInBlock;
    public CurrentlyInBlock feetInBlock;
    
    [Space(10)]
    public float speedUnderwater = 3.0f;
    public float acceleratedUnderwater = 6f;
    public float jumpHeightUnderwater = 5f;
    public float gravityUnderwater = -4.9f;
    
    Transform groundCheck;
    private Vector3 velocity;
    private CharacterController controller;
    private Animator animator;
    private bool isGrounded;

    private float jumpCooldownTimePassed = 1;
    private List<MovementEffect> movementEffects = new List<MovementEffect>();
    private List<MovementEffect> usedMovementEffects = new List<MovementEffect>();

    private bool stopGravityEffect = false;
    private Vector3 forceEffect = Vector3.zero;
    
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        groundCheck = transform.Find("GroundCheck");
    }

    private void Update()
    {
        if(PauseMenu.pause || groundCheck == null) 
            return;

        LayerMask terrainMask = 1 << LayerMask.NameToLayer("Terrain");
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, terrainMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }


        float currentGravity = gravity;
        
        if (isMainController)
        {
            UpdateMovementEffects();
            
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical   = Input.GetAxisRaw("Vertical");

            bool acclerated = false;
            bool underwater = bodyInBlock?.inBlock == BlockType.Water;
            float currentSpeed = 0;
            float currentJumpHeight = underwater ? jumpHeightUnderwater : jumpHeight;
            currentGravity = underwater ? gravityUnderwater : gravity;
            
            bool jumpCooldownIsOver = false;
            jumpCooldownTimePassed += Time.deltaTime;
        
            if (jumpCooldownTimePassed > jumpCooldown)
                jumpCooldownIsOver = true;

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                acclerated = true;
                currentSpeed = underwater ? acceleratedUnderwater : acceleratedSpeed; 
            }
            else
            {
                currentSpeed = underwater ? speedUnderwater : speed; 
            }

            if (animator != null)
            {
                animator?.SetBool("Walking", Input.GetAxisRaw("Vertical") != 0);
            }
            
            Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
            if (isMainController && Input.GetButton("Jump") && jumpCooldownIsOver && (isGrounded || underwater))
            {
                jumpCooldownTimePassed = 0;
                if (!underwater)
                {
                    if (acclerated)
                    {
                        AddEffect(MovementEffectType.force, 0.6f, moveDirection * (jumpSpeed * 1.4f));
                    }
                    else
                    {
                        AddEffect(MovementEffectType.force, 0.5f, moveDirection * jumpSpeed);
                    }
                }

                if (underwater && feetInBlock.inBlock != BlockType.Water)
                {
                    AddEffect(MovementEffectType.force, 1f, new Vector3(0, 0.5f, 0) * jumpSpeed);
                }
                
                currentJumpHeight = (underwater && Random.Range(0, 4) > 1) ? jumpHeight / 2 : jumpHeight;
                velocity.y = Mathf.Sqrt(currentJumpHeight * -2 * currentGravity);
            }

            //If inside of block - move up.
            if (headInBlock.inBlock == BlockType.Air && bodyInBlock.inBlock != BlockType.Air && bodyInBlock.inBlock != BlockType.Water)
                controller.Move(new Vector3(0, 1f, 0));
            
            controller.Move((moveDirection * currentSpeed * Time.deltaTime) + (forceEffect * Time.deltaTime));
        }
        
        if (isChase)
        {
            LayerMask entity = 1 << LayerMask.NameToLayer("Entity");
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 100, entity);
            PlayerMovement target = GetClosestEnemy(hitColliders, 100);
            if (target != null)
            {
                Vector3 _direction = (target.transform.position - transform.position).normalized;
                Quaternion _lookRotation = Quaternion.LookRotation(_direction, Vector3.up);
                transform.rotation = Quaternion.Euler(0, _lookRotation.eulerAngles.y, _lookRotation.eulerAngles.z);

                float xRotation = transform.eulerAngles.x > 180 ? transform.eulerAngles.x - 360 : transform.eulerAngles.x;
                float xRotationClamp = Mathf.Clamp(xRotation, -60f, 60f);
                transform.eulerAngles = new Vector3(xRotationClamp, transform.eulerAngles.y, transform.eulerAngles.z);

                Vector3 t = target.transform.position - transform.position;
                float dist = t.x * t.x + t.y * t.y + t.z * t.z;
                if (dist > 9)
                {
                    _direction.y = 0;
                    controller.Move(_direction * speed * Time.deltaTime);
                }
            }
            
            if (stopGravityEffect)
            {
                currentGravity = 0;
                velocity.y = Mathf.Clamp(velocity.y, 0, Single.PositiveInfinity);
            }
        }

        velocity.y += currentGravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private PlayerMovement GetClosestEnemy(Collider[] enemies, float radius)
    {
        PlayerMovement cub = null;
        float minDist = radius * radius;
        Vector3 currentPos = transform.position;
        foreach (Collider c in enemies)
        {
            if (c.gameObject == gameObject)
                continue;
            PlayerMovement cube = c.GetComponent<PlayerMovement>();
            if (cube != null && cube.isMainController)
            {
                Vector3 t = c.transform.position - currentPos;
                float dist = t.x * t.x + t.y * t.y + t.z * t.z;
                if (dist < minDist)
                {
                    cub = cube;
                    minDist = dist;
                }
            }
        }
        return cub;
    }

    public void AddEffect(MovementEffectType effectType, float duration)
    {
        MovementEffect effect = new MovementEffect()
        {
            duration = duration,
            effectType = effectType
        };
        
        movementEffects.Add(effect);
    }
    
    public void AddEffect(MovementEffectType effectType, float duration, Vector3 force)
    {
        MovementEffect effect = new MovementEffect()
        {
            duration = duration,
            force = force,
            effectType = effectType
        };
        
        movementEffects.Add(effect);
    }

    private void UpdateMovementEffects()
    {
        stopGravityEffect = false;
        forceEffect = Vector3.zero;

        for (int i = 0; i < movementEffects.Count; i++)
        {
            var temp = movementEffects[i];

            switch (movementEffects[i].effectType)
            {
                case MovementEffectType.force:
                    forceEffect += movementEffects[i].force * Mathf.Clamp(movementEffects[i].duration * 4, 0, 1);
                    break;
                
                case MovementEffectType.stopGravity:
                    stopGravityEffect = true;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            temp.duration -= Time.deltaTime;
            if (temp.duration <= 0)
                continue;

            movementEffects[i] = temp;
            usedMovementEffects.Add(temp);
        }
        movementEffects.Clear();
        
        for (int i = 0; i < usedMovementEffects.Count; i++)
        {
            movementEffects.Add(usedMovementEffects[i]);
        }
        usedMovementEffects.Clear();
    }
    
    struct MovementEffect
    {
        public MovementEffectType effectType;
        public Vector3 force;
        public float duration;
    }

    public enum MovementEffectType
    {
        force,
        stopGravity
    }
}