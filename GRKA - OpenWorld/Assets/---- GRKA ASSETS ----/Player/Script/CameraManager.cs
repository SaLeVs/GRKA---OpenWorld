using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    InputManager inputManager;
    PlayerManager playerManager;

    public Transform targetTransform;
    public Transform cameraPivot;
    public Transform cameraTransform;
    private float defaultPosition;
    private Vector3 cameraFollowVelocity = Vector3.zero;
    private Vector3 cameraVectorPosition;
    public LayerMask collisionLayers;

    public float cameraCollisionOffSet = 0.2f;
    public float minimiumCollisionOffSet = 0.2f;
    public float cameraCollisionRadius = 4;
    public float cameraFollowSpeed = 0.2f;
    public float cameraLookSpeed = 2;
    public float cameraPivotSpeed = 2;

    public float lookAngle;
    public float pivotAngle;
    public float minimumPivotAngle = -35;
    public float maximumPivotAngle = 35;

    [Header("Camera Follow Targets")]
    public GameObject player;
    public Transform aimedCameraPosition;
    public float AimCameraSmoothTime = 3f;

    

    private void Awake()
    {
        inputManager = FindObjectOfType<InputManager>();
        targetTransform = FindObjectOfType<PlayerManager>().transform;
        playerManager = player.GetComponent<PlayerManager>();
        cameraTransform = Camera.main.transform;
        defaultPosition = cameraTransform.localPosition.z;
    }

    public void HandleAllCameraMovement()
    {
        
          FollowTarget();
          if (playerManager.isDead)
             return;
          RotateCamera();
          HandleCameraCollisions();
        
    }

    private void FollowTarget()
    {
        if (playerManager.isAiming)
        {
            Vector3 targetPosition = Vector3.SmoothDamp(transform.position, aimedCameraPosition.transform.position, ref cameraFollowVelocity, cameraFollowSpeed);
            transform.position = targetPosition;
        }
        else
        {
            Vector3 targetPosition = Vector3.SmoothDamp(transform.position, targetTransform.position, ref cameraFollowVelocity, cameraFollowSpeed);
            transform.position = targetPosition;
        }
        
        
    }

    private void RotateCamera()
    {
        
        if(playerManager.isAiming)
        {

            Vector3 rotation;
            Quaternion targetRotation;

            cameraPivot.localRotation = Quaternion.Euler(0, 0, 0);

            lookAngle = lookAngle + (inputManager.cameraInputX * cameraLookSpeed);
            pivotAngle = pivotAngle - (inputManager.cameraInputY * cameraPivotSpeed);
            pivotAngle = Mathf.Clamp(pivotAngle, minimumPivotAngle, maximumPivotAngle);

            rotation = Vector3.zero;
            rotation.y = lookAngle;
            targetRotation = Quaternion.Euler(rotation);
            transform.rotation = targetRotation;

            rotation = Vector3.zero;
            rotation.x = pivotAngle;
            targetRotation = Quaternion.Euler(rotation);
            targetRotation = Quaternion.Slerp(cameraPivot.localRotation, targetRotation, AimCameraSmoothTime);
            cameraTransform.transform.localRotation = targetRotation;
        }
        else
        {
          Vector3 rotation;
          Quaternion targetRotation;

          cameraTransform.transform.localRotation = Quaternion.Euler(0,0,0);

          lookAngle = lookAngle + (inputManager.cameraInputX * cameraLookSpeed);
          pivotAngle = pivotAngle - (inputManager.cameraInputY * cameraPivotSpeed);
          pivotAngle = Mathf.Clamp(pivotAngle, minimumPivotAngle, maximumPivotAngle);

          rotation = Vector3.zero;
          rotation.y = lookAngle;
          targetRotation = Quaternion.Euler(rotation);
          transform.rotation = targetRotation;

          rotation = Vector3.zero;
          rotation.x = pivotAngle;
          targetRotation = Quaternion.Euler(rotation);
          cameraPivot.localRotation = targetRotation;
        }

        
    }


    private void HandleCameraCollisions()
    {
        float targetPosition = defaultPosition;
        RaycastHit hit;

        Vector3 direction = cameraTransform.position - cameraPivot.position;
        direction.Normalize();

        if (Physics.SphereCast(cameraPivot.transform.position, cameraCollisionRadius, direction, out hit, Mathf.Abs(targetPosition), collisionLayers))
        {
            float distance = Vector3.Distance(cameraPivot.position, hit.point);
            targetPosition =- (distance - cameraCollisionOffSet);
        }

        if(Mathf.Abs(targetPosition) < minimiumCollisionOffSet)
        {
            targetPosition = targetPosition - minimiumCollisionOffSet;
        }

        cameraVectorPosition.z = Mathf.Lerp(cameraTransform.localPosition.z, targetPosition, 0.2f);
        cameraTransform.localPosition = cameraVectorPosition;
    }
}
