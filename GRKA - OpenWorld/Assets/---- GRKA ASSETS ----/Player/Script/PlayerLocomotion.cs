using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerLocomotion : MonoBehaviour
{
    InputManager inputManager;
    PlayerManager playerManager;
    AnimatorManager animatorManager;

    Vector3 moveDirection;
    public Vector3 chDirection;
    Transform cam;
    Rigidbody rb;

    [Header("Falling")]
    public float inAirTIme;
    public float learpingVelocity;
    public float fallingVelocity;
    public float rayCAstHeightOffSet = 0.5f;
    public float maxDistance = 1f;
    public float teste;
    public LayerMask groundLayer;


    [Header("Movement Flags")]
    public bool isSpriting;
    public bool isWalking;
    public bool isGrounded;
    public bool isJumping;
    public bool isClimbing;

    [Header("Movement Speeds")]
    public float walkingSpeed = 7;
    public float runningSpeed = 7;
    public float spritingSpeed = 20;
    public float rotationSpeed = 15;

    [Header("Jumps")]
    public float jumpHeight = 3;
    public float gravityIntensity = -15;
    public float jumpSpeed;

    [Header("Movement Variables")]
    Quaternion targetRotation;
    Quaternion playerRotation;

    public void Awake()
    {

        playerManager = GetComponent<PlayerManager>();
        animatorManager = GetComponent<AnimatorManager>();
        inputManager = GetComponent<InputManager>();
        rb = GetComponent<Rigidbody>();
        cam = Camera.main.transform;
    }

    public void HandleAllMovement()
    {
        HandleFallingAndLanding();
        HandleClimbingLadders();

        if (playerManager.isInteracting)
          return;

        HandleMovement();
        HandleRotation();
        
    }

    private void HandleMovement()
    {
        if (isJumping)
          return;
        moveDirection = cam.forward * inputManager.verticalInput;
        moveDirection = moveDirection + cam.right * inputManager.horizontalInput;
        moveDirection.Normalize();
        moveDirection.y = 0;

        if(isSpriting)
        {
            moveDirection = moveDirection * spritingSpeed;
        }
        else
        {
            if (inputManager.moveAmount >= 0.5f)
            {
                moveDirection = moveDirection * runningSpeed;
            }
            else
            {
                moveDirection = moveDirection * walkingSpeed;
            }
        }

        if (isWalking)
        {
            moveDirection = moveDirection * walkingSpeed;
        }
        else
        {
            if (inputManager.moveAmount >= 0.5f)
            {
                moveDirection = moveDirection * runningSpeed;
            }
            
        }



        Vector3 movementVelocity = moveDirection;
        rb.velocity = movementVelocity;
        
    }

    private void HandleRotation()
    {

        if (isJumping)
            return;

        if (playerManager.isAiming)
        {
            
            targetRotation = Quaternion.Euler(0, cam.eulerAngles.y, 0);
            playerRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            transform.rotation = playerRotation;

        }
        else
        {
            Vector3 targetDirection = Vector3.zero;
            targetDirection = cam.forward * inputManager.verticalInput;
            targetDirection = targetDirection + cam.right * inputManager.horizontalInput;
            targetDirection.Normalize();
            targetDirection.y = 0;

            if (targetDirection == Vector3.zero)
            {
                targetDirection = transform.forward;
            }

            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            Quaternion playerRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            transform.rotation = playerRotation;
        }
          
    }

    private void HandleFallingAndLanding()
    {
        RaycastHit hit;
        Vector3 rayCastOrigin = transform.position;
        Vector3 targetPosition;
        rayCastOrigin.y = rayCastOrigin.y + rayCAstHeightOffSet;
        targetPosition = transform.position;


        if(!isGrounded && !isJumping)
        {
            if(!playerManager.isInteracting)
            {
                animatorManager.PlayTargetAnimation("Falling", true);
            }

            inAirTIme = inAirTIme + Time.deltaTime;
            rb.AddForce(transform.forward * learpingVelocity);
            rb.AddForce(-Vector3.up * fallingVelocity * inAirTIme); 

        }

        if(Physics.SphereCast(rayCastOrigin, 0.2f, -Vector3.up, out hit, maxDistance, groundLayer))
        {
            if(!isGrounded && !playerManager.isInteracting)
            {
                animatorManager.PlayTargetAnimation("Land", true);
            }
            Vector3 rayCastHitPoint = hit.point;
            targetPosition.y = rayCastHitPoint.y;
            inAirTIme = 0;
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        if (isGrounded && !isJumping)

        {
            if(playerManager.isInteracting || inputManager.moveAmount > 0)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime / 0.1f);
            }
            else
            {
                transform.position = targetPosition;
            }
        }

        chDirection = targetPosition;
    }

    public void HandleJumping()
    {
        if (isGrounded)
        {
            animatorManager.animator.SetBool("isJumping", true);
            animatorManager.PlayTargetAnimation("Jump", false);

            float jumpingVelocity = Mathf.Sqrt(-2 * gravityIntensity * jumpHeight);
            Vector3 playerVelocity = moveDirection * jumpSpeed;
            playerVelocity.y = jumpingVelocity;
            rb.velocity = playerVelocity;
        }
    }

    public void HandleClimbingLadders()
    {

        float avoidFloorDistance = 1f;
        float ladderGrabDistance = 1f;
        Vector3 forwardDirection = transform.forward;
        chDirection = transform.position;

        if (Physics.Raycast(transform.position + Vector3.up * avoidFloorDistance, forwardDirection, out RaycastHit raycastHit, ladderGrabDistance))
        {
            if (raycastHit.transform.TryGetComponent(out Ladders ladders))
            {
                
                isClimbing = true;

                if(isClimbing)
                {
                  
                    chDirection.x = 0f;
                    chDirection.y = chDirection.z;
                    chDirection.z = 0f;
                   isGrounded = true;
                }
                
            }
            else
            {
                isClimbing = false;
            }
        }
    }

}
