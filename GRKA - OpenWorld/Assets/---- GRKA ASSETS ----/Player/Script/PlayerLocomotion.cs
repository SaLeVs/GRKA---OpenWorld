using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;

public class PlayerLocomotion : MonoBehaviour
{
    InputManager inputManager;
    PlayerManager playerManager;
    AnimatorManager animatorManager;
    Animator animator;

    Vector3 moveDirection;
    public Vector3 lastGrabLadderDirection;
    Transform cam;
    public Rigidbody rb;
    [SerializeField] private Transform playerCameraTransform;
    [SerializeField] private GameObject dashIndicator; // Indicador de teleporte

    [Header("Falling")]
    public float inAirTIme;
    public float learpingVelocity;
    public float fallingVelocity;
    public float rayCAstHeightOffSet = 0.5f;
    public float maxDistance = 1f;
    public LayerMask groundLayer;

    [Header("Movement Flags")]
    public bool isSpriting;
    public bool isWalking;
    public bool isGrounded;
    public bool isJumping;
    public bool isClimbing;
    public bool isDashing;

    [Header("Movement Speeds")]
    public float walkingSpeed = 7;
    public float runningSpeed = 7;
    public float spritingSpeed = 20;
    public float rotationSpeed = 15;
    public float dashSpeed = 20;
    public float dashSpeedInGround = 100;
    public float climbingSpeed = 4;

    [Header("Jumps")]
    public float jumpHeight = 3;
    public float gravityIntensity = -15;
    public float jumpSpeed;

    [Header("Movement Variables")]
    Quaternion targetRotation;
    Quaternion playerRotation;
    public float rot;

    // Refer�ncia ao topo da escada
    private Transform ladderTop;

    public void Awake()
    {
        playerManager = GetComponent<PlayerManager>();
        animatorManager = GetComponent<AnimatorManager>();
        inputManager = GetComponent<InputManager>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        cam = Camera.main.transform;
        isClimbing = false;
        isDashing = false;

        // Desativar o indicador inicialmente
        if (dashIndicator != null)
        {
            dashIndicator.SetActive(false);
        }
    }

    public void HandleAllMovement()
    {
        if (playerManager.isDead)
            return;

        HandleFallingAndLanding();
        if (playerManager.isInteracting)
            return;
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        if (isJumping || isClimbing)
            return;

        moveDirection = cam.forward * inputManager.verticalInput;
        moveDirection = moveDirection + cam.right * inputManager.horizontalInput;
        moveDirection.Normalize();
        moveDirection.y = 0;

        if (isSpriting)
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
        if (isJumping || isClimbing)
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
        // Verifica se est� escalando, se sim, n�o executa o c�digo de queda
        if (isClimbing)
            return;

        RaycastHit hit;
        Vector3 rayCastOrigin = transform.position;
        Vector3 targetPosition;
        rayCastOrigin.y = rayCastOrigin.y + rayCAstHeightOffSet;
        targetPosition = transform.position;

        if (!isGrounded && !isJumping)
        {
            if (!playerManager.isInteracting && !isClimbing)
            {
                animatorManager.PlayTargetAnimation("Falling", true);
            }

            inAirTIme = inAirTIme + Time.deltaTime;
            rb.AddForce(transform.forward * learpingVelocity);
            rb.AddForce(-Vector3.up * fallingVelocity * inAirTIme);
        }

        if (Physics.SphereCast(rayCastOrigin, 0.2f, -Vector3.up, out hit, maxDistance, groundLayer))
        {
            if (!isGrounded && !playerManager.isInteracting && !isClimbing)
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

        if (isGrounded && !isJumping && !isClimbing)
        {
            if (playerManager.isInteracting || inputManager.moveAmount > 0)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime / 0.1f);
            }
            else
            {
                transform.position = targetPosition;
            }
        }
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

    public void HandleClimbing()
    {
        rot = Mathf.Atan2(inputManager.movementInput.x, inputManager.movementInput.y) * Mathf.Rad2Deg + cam.eulerAngles.y;
        Vector3 targetDirection = transform.forward;

        if (!isClimbing && PlayerPrefs.GetInt("Shire1Finishe") == 1)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                float avoidFloorDistance = 0.4f;
                float ladderGrabDistance = 1f;

                if (Physics.Raycast(transform.position + Vector3.up * avoidFloorDistance, targetDirection, out RaycastHit raycastHit, ladderGrabDistance))
                {
                    if (raycastHit.transform.TryGetComponent(out Ladders ladders))
                    {
                        HandleGrabLadders(targetDirection, ladders.topTransform);
                    }
                }
            }
        }
        else
        {
            float avoidFloorDistance = 0.4f;
            float ladderGrabDistance = 1f;

            if (Physics.Raycast(transform.position + Vector3.up * avoidFloorDistance, targetDirection, out RaycastHit raycastHit, ladderGrabDistance))
            {
                if (raycastHit.transform.TryGetComponent(out Ladders ladders))
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        HandleDropLadders();
                        animator.SetBool("isClimbing", false);
                    }
                    else if (Input.GetKeyDown(KeyCode.Space))
                    {
                        HandleDropLadders();
                        HandleJumping();
                        animator.SetBool("isClimbing", false);
                    }
                }
                else
                {
                    HandleDropLadders();
                    animator.SetBool("isClimbing", false);
                }
            }
            else
            {
                HandleDropLadders();
                animator.SetBool("isClimbing", false);
            }

            if (isClimbing)
            {
                // Verificar se est� perto do topo
                if (ladderTop != null && Vector3.Distance(transform.position, ladderTop.position) < 0.5f)
                {
                    // Parar de escalar e subir no topo
                    HandleReachTop();
                }
                else
                {
                    targetDirection.x = 0f;
                    targetDirection.y = inputManager.verticalInput * climbingSpeed;
                    targetDirection.z = 0f;
                    rb.velocity = targetDirection;
                    isGrounded = true;
                    animator.SetBool("isClimbing", true);
                }
            }
        }
    }

    public void HandleGrabLadders(Vector3 lastGrabLadderDirection, Transform ladderTop)
    {
        rb.useGravity = false;
        isClimbing = true;
        this.lastGrabLadderDirection = lastGrabLadderDirection;
        this.ladderTop = ladderTop;
    }

    public void HandleDropLadders()
    {
        isClimbing = false;
        animator.SetBool("isClimbing", false);
        rb.useGravity = true;
        ladderTop = null;
    }

    private void HandleReachTop()
    {
        isClimbing = false;
        animator.SetBool("isClimbing", false);
        rb.useGravity = true;

        // Ajusta a posi��o do jogador para o topo da escada
        transform.position = ladderTop.position;
        rb.velocity = Vector3.zero; // Para o movimento
    }

    public void HandleDash()
    {
        if (!isDashing && isGrounded == true)
        {
            rb.AddForce(transform.forward * dashSpeedInGround, ForceMode.Force);
            isDashing = true;
            StartCoroutine(resetDash());
            Debug.Log("deuDash");
        }
        if (!isDashing && isGrounded == false)
        {
            rb.AddForce(transform.forward * dashSpeed, ForceMode.Force);
            isDashing = true;
            StartCoroutine(resetDash());
            Debug.Log("deuDash");
        }
    }

    IEnumerator resetDash()
    {
        yield return new WaitForSeconds(3);
        isDashing = false;
        Debug.Log("Saiudaespera");
    }

    private void UpdateDashIndicator()
    {
        if (dashIndicator == null)
            return;

        if (Input.GetKey(KeyCode.V) && PlayerPrefs.GetInt("Shire2Finishe") == 1)
        {
            Vector3 dashPosition = transform.position;
            if (isGrounded)
            {
                dashPosition += transform.forward * 10f; // Alcance do dash quando estiver no ch�o
            }
            else
            {
                dashPosition += transform.forward * 5f; // Alcance do dash quando estiver no ar
            }
            dashPosition.y += 0.1f; // Deslocamento vertical para elevar o indicador
            dashIndicator.transform.position = dashPosition;
            dashIndicator.SetActive(true);
        }
        else
        {
            dashIndicator.SetActive(false);
        }
    }

    public void Update()
    {
        HandleClimbing();
        UpdateDashIndicator();
    }
}