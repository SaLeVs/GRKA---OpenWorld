using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLocomotion : MonoBehaviour
{
    InputManager inputManager;

    Vector3 moveDirection;
    Transform cam;
    Rigidbody rb;

    public bool isSpriting;
    

    [Header("Movement Speeds")]
    public float walkingSpeed = 7;
    public float spritingSpeed = 20;
    public float runningSpeed = 7;
    public float rotationSpeed = 15;

    public void Awake()
    {
        inputManager = GetComponent<InputManager>();
        rb = GetComponent<Rigidbody>();
        cam = Camera.main.transform;
    }

    public void HandleAllMovement()
    {
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
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

        

        Vector3 movementVelocity = moveDirection;
        rb.velocity = movementVelocity;

    }

    private void HandleRotation()
    {
        Vector3 targetDirection = Vector3.zero;
        targetDirection = cam.forward * inputManager.verticalInput;
        targetDirection = targetDirection + cam.right * inputManager.horizontalInput;
        targetDirection.Normalize();
        targetDirection.y = 0;

        if(targetDirection == Vector3.zero)
        {
            targetDirection = transform.forward;
        }

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Quaternion playerRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        transform.rotation = playerRotation;    
    }
}
