using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonController : MonoBehaviour
{
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
    
   // [Header("Camera Settings")]
    

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

    private float yawOffset = 0f;
    private float pitchOffset = 20f;
    private Vector3 cameraVelocityRef;
    
    #endregion
    
    #region Variable Setup - States

    private bool canMove = true;

    #endregion
    
    #region Setup

    private void Awake()
    {
        if (playerTransform == null)
        {
            Debug.LogError("[Player] Error: PlayerTransform not found");
            return;
        }
        
        characterController = playerTransform.GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("[Player] Error: CharacterController not found");
            return;
        }
        
        
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
            HandleCamera();
        }
    }

    #endregion

    #region Movement

    private void HandleMovement()
    {
        HandleGroundCheck();

        Vector2 moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        float turnInput = moveInput.x;
        float throttleInput = moveInput.y;
        
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
        
        // Movement
        Vector3 currentHorizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        
        if (Mathf.Abs(throttleInput) > 0.1f)
        {
            currentHorizontalVelocity += playerTransform.forward * (throttleInput * acceleration * Time.deltaTime);
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

    #region Camera

    private void HandleCamera()
    {
       
    }

    #endregion
}
