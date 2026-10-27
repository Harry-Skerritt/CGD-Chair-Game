using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonController : MonoBehaviour
{
    [Header("Player Camera")]
    [SerializeField] private CinemachineCamera playerCamera;
    
    [Header("Inputs")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    
    [Header("Player Transform")]
    [SerializeField] private Transform playerTransform;

    [Header("Movement Settings")] 
    [SerializeField] private float maxSpeed = 25.0f;
    [SerializeField] private float acceleration = 12.0f;
    [SerializeField] private float friction = 2.5f;

    [Header("Rotation Settings")] 
    [SerializeField] private float maxTurnSpeed = 350f;
    [SerializeField] private float turnAcceleration = 600f;
    [SerializeField] private float turnDeceleration = 400f;

    [Header("Camera Settings")] 
    [Tooltip("Will auto populate if left blank")] [SerializeField] private Transform cameraFollowTarget;
    [SerializeField] private float cameraLagTime = 0.25f;   
    [SerializeField] private Vector3 cameraStartRotation = new Vector3(20f, 0f, 0f);
    [SerializeField] private float upLimit = 20.0f;
    [SerializeField] private float downLimit = -20.0f;
    [SerializeField] private float pitchSpeed = 150f;

    [Header("Camera Auto Return Settings")]
    [SerializeField] private bool cameraAutoReturns = false;
    [SerializeField] private float returnToStartDelay = 0.5f;
    [SerializeField] private float returnToStartSpeed = 3.0f;
    

    [Header("Gravity")] 
    [Tooltip("MUST be a positive value")] [SerializeField] private float gravity = 20f;
    [Tooltip("MUST be a positive value")] [SerializeField] private float downForce = 2f;
    
    
    #region Variable Setup - Movement
    
    private CharacterController characterController;
    private Vector3 currentVelocity = Vector3.zero;
    private float currentThrottle = 0f;
    private float currentTurnSpeed = 0f;
    
    #endregion

    #region Variable Setup - Camera

    private float smoothedCameraYaw = 0f;
    private float cameraYawVelocity = 0f;
    
    private float currentPitch = 0f;
    private float targetPitch = 0f;
    private float pitchVelocity = 0f;
    private float returnTimer = 0f;
    private bool isReturningToStart = false;
    private float defaultPitch = 0f;
    
    #endregion
    
    #region Variable Setup - States

    private bool canMove = true;

    #endregion
    
    #region Setup

    private void Awake()
    {
        // NECESSARY references
        if (playerTransform == null)
        {
            Debug.LogError("[Player] Error: PlayerTransform not found");
            return;
        }
        
        characterController = transform.GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("[Player] Error: CharacterController not found");
            return;
        }
        
        // Camera
        if (cameraFollowTarget == null)
        {
            GameObject targetObj = new GameObject("CameraFollowTarget");
            cameraFollowTarget = targetObj.transform;
        }

        if (playerCamera != null) playerCamera.Follow = cameraFollowTarget;
        smoothedCameraYaw = playerTransform.eulerAngles.y;
        
        defaultPitch = cameraStartRotation.x;
        currentPitch = defaultPitch;
        targetPitch = defaultPitch;
        
        
        // Cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        moveAction?.action.Enable();
        lookAction?.action.Enable();
    }


    private void OnDisable()
    {
        moveAction.action.Disable();
        lookAction.action.Disable();
    }

    #endregion

    #region State Setters / Getters

    public void SetPlayerEnabled(bool enabled)
    {
        canMove = enabled;
    }
    
    public bool CanMove() => canMove;

    #endregion
    
    #region Updates

    private void Update()
    {
        if (canMove)
        {
            HandleMovement();
        }
    }

    private void LateUpdate()
    {
        if (canMove)
        {
            HandleLook();
            HandleCamera();
        }
    }

    #endregion

    #region Movement

    private void HandleMovement()
    {
        HandleGroundCheck();

        Vector2 moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        float throttleInputX = moveInput.x;
        float throttleInputZ = moveInput.y;
        
        // Movement
        Vector3 currentHorizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        if (Mathf.Abs(throttleInputZ) > 0.1f)
        {
            currentHorizontalVelocity += playerTransform.forward * (throttleInputZ * acceleration * Time.deltaTime);
            currentHorizontalVelocity = Vector3.ClampMagnitude(currentHorizontalVelocity, maxSpeed);
        }
        else
        {
            currentHorizontalVelocity = 
                Vector3.MoveTowards(currentHorizontalVelocity, Vector3.zero, friction * Time.deltaTime);
        }

        if (Mathf.Abs(throttleInputX) > 0.1f)
        {
            currentHorizontalVelocity += playerTransform.right * (throttleInputX * acceleration * Time.deltaTime);
            currentHorizontalVelocity = Vector3.ClampMagnitude(currentHorizontalVelocity, maxSpeed);
        }
        else
        {
            currentHorizontalVelocity = 
                Vector3.MoveTowards(currentHorizontalVelocity, Vector3.zero, friction * Time.deltaTime);
        }
        
        // CC Movement
        currentVelocity.x = currentHorizontalVelocity.x;
        currentVelocity.z = currentHorizontalVelocity.z;
        currentVelocity.y = -gravity * Time.deltaTime;
        
        characterController.Move(currentVelocity * Time.deltaTime);
    }

    private void HandleGroundCheck()
    {
        if (characterController.isGrounded && currentVelocity.y < 0)
        {
            currentVelocity.y = -downForce;
        }
    }

    #endregion

    #region Camera / Look

    private void HandleLook()
    {
        Vector2 lookInput = lookAction != null ? lookAction.action.ReadValue<Vector2>() : Vector2.zero;
        float turnInput = lookInput.x;
        
        // Rotation
        if (Mathf.Abs(turnInput) > 0.1f)
        {
            currentTurnSpeed =
                Mathf.MoveTowards(currentTurnSpeed, turnInput * maxTurnSpeed, turnAcceleration * Time.deltaTime);
        }
        else
        {
            currentTurnSpeed =
                Mathf.MoveTowards(currentTurnSpeed, 0f, turnDeceleration * Time.deltaTime);
        }
        
        playerTransform.Rotate(0, currentTurnSpeed * Time.deltaTime, 0);
        
    }

    private void HandleCamera()
    {
        // Align with Chair Pos
        if (cameraFollowTarget == null) return;
        cameraFollowTarget.position = playerTransform.position;
        
        // Yaw Control
        float targetYaw = playerTransform.eulerAngles.y;
        smoothedCameraYaw = Mathf.SmoothDampAngle(smoothedCameraYaw, targetYaw, ref cameraYawVelocity, cameraLagTime);
        
        // Manual Pitch
        Vector2 lookInput = lookAction != null ? lookAction.action.ReadValue<Vector2>() : Vector2.zero;
        float pitchInput = lookInput.y;

        if (Mathf.Abs(pitchInput) > 0.1f)
        {
            targetPitch -= pitchInput * pitchSpeed * Time.deltaTime;
            targetPitch = Mathf.Clamp(targetPitch, downLimit, upLimit);

            if (cameraAutoReturns)
            {
                returnTimer = returnToStartDelay;
                isReturningToStart = false;
            }
           
        }
        else
        {
            if (cameraAutoReturns)
            {
                if (returnTimer > 0f) returnTimer -= Time.deltaTime;
                else isReturningToStart = true;
            }

        }
        
        if (isReturningToStart && cameraAutoReturns) 
            targetPitch = Mathf.SmoothDamp(targetPitch, defaultPitch, ref pitchVelocity, 1f / returnToStartSpeed);
        
        
        currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, 0.05f);
       
        // Apply
        Quaternion yawRotation = Quaternion.Euler(0f, smoothedCameraYaw, 0f);
        Quaternion pitchRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        cameraFollowTarget.rotation = yawRotation * pitchRotation;
    }
    

    #endregion
}
