using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3.0f; 
    public float runSpeed = 6.0f;
    public float jumpSpeed = 8.0f;
    public float gravity = 25.0f; 
    public float rotationSpeed = 15.0f; 
    public float acceleration = 8.0f;

    [Header("Combat Settings")]
    public float attackLungeSpeed = 4.0f; 
    public float comboResetTime = 1.2f; 

    [Header("Audio")]
    public AudioSource playerAudio; 
    [Tooltip("Add reply audios here. e.g., 0 = Soul Shivraj, 1 = Physical Shivraj")]
    public AudioClip[] enemyReplies;

    private CharacterController controller;
    private Animator animator;
    private Transform mainCameraTransform;
    private CameraFollow cameraFollowScript;
    
    private Vector3 moveDirection = Vector3.zero;
    private float verticalVelocity = 0f;
    private float currentLerpSpeed = 0f;

    public bool isFrozen = false; // Cinematic scene ke liye
    private bool isAttacking = false;
    private int comboStep = 0;
    private int slashComboStep = 0;
    private float lastAttackTime = 0f;

    void Start()
    {
        // Add AAA Voice Effect
        gameObject.AddComponent<AAAVoiceEffect>();

        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
            cameraFollowScript = Camera.main.GetComponent<CameraFollow>();
        }
    }

    void Update()
    {
        HandleAttacks(); 
        HandleMovement();
    }

    void HandleMovement()
    {
        float horizontal = 0f;
        float vertical = 0f;
        bool isRunning = false;
        bool jumpPressed = false;

        if (Keyboard.current != null && !isAttacking && !isFrozen)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
            
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;

            isRunning = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        Vector3 inputDirection = Vector3.zero;

        if (mainCameraTransform != null)
        {
            Vector3 camForward = mainCameraTransform.forward;
            Vector3 camRight = mainCameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            inputDirection = (camForward * vertical + camRight * horizontal).normalized;
        }
        else
        {
            inputDirection = new Vector3(horizontal, 0.0f, vertical).normalized;
        }

        float targetSpeed = 0f;
        if (inputDirection.magnitude >= 0.1f)
        {
            targetSpeed = isRunning ? runSpeed : walkSpeed;
        }
        
        currentLerpSpeed = Mathf.Lerp(currentLerpSpeed, targetSpeed, acceleration * Time.unscaledDeltaTime);

        if (isAttacking)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            if (controller.isGrounded)
            {
                if (stateInfo.normalizedTime < 0.35f) 
                {
                    moveDirection = transform.forward * attackLungeSpeed;
                }
                else
                {
                    moveDirection.x = 0;
                    moveDirection.z = 0;
                }
            }
        }
        else if (inputDirection.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.unscaledDeltaTime);

            moveDirection = inputDirection * currentLerpSpeed;
            animator.SetFloat("Speed", isRunning ? 1.0f : 0.5f, 0.1f, Time.unscaledDeltaTime);
        }
        else
        {
            moveDirection.x = 0;
            moveDirection.z = 0;
            animator.SetFloat("Speed", 0.0f, 0.1f, Time.unscaledDeltaTime);
        }

        if (controller.isGrounded)
        {
            verticalVelocity = -5f; 

            if (!isAttacking && jumpPressed)
            {
                verticalVelocity = jumpSpeed;
                animator.SetTrigger("Jump");
                
                if(cameraFollowScript != null) cameraFollowScript.TriggerShake(0.1f, 0.05f);
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.unscaledDeltaTime;
        }

        moveDirection.y = verticalVelocity;
        controller.Move(moveDirection * Time.unscaledDeltaTime);
    }

    void HandleAttacks()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool currentlyPlayingAttack = stateInfo.IsTag("Attack");

        if (currentlyPlayingAttack)
        {
            isAttacking = true;
        }
        else
        {
            isAttacking = false;
            // User request: memory store rakhna hai, combo timeout nahi hoga.
            // Isliye timeout reset logic hata diya gaya hai.
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isFrozen)
        {
            // Pehle wala attack agar 60% se jyada ho gaya ho, tabhi doosra attack input lega (Combo flow ke liye)
            if (currentlyPlayingAttack && stateInfo.normalizedTime < 0.6f) return;

            bool isCtrlHeld = false;
            if (Keyboard.current != null)
            {
                isCtrlHeld = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed || 
                             Keyboard.current.leftCommandKey.isPressed || Keyboard.current.rightCommandKey.isPressed;
            }

            if (isCtrlHeld)
            {
                // Heavy Slash Attack Combo (Ctrl + Click) -> 1, phir 2
                slashComboStep++;
                if (slashComboStep > 2) slashComboStep = 1;

                animator.SetInteger("AttackType", slashComboStep);
                animator.SetTrigger("SlashAttack");
                lastAttackTime = Time.time;
                
                StartCoroutine(CinematicSlowMo(0.3f, 0.2f)); 
                if(cameraFollowScript != null) cameraFollowScript.TriggerShake(0.2f, 0.1f);
            }
            else
            {
                // Sword Combo System (Click) -> 1, phir 2, phir 3
                comboStep++;
                if (comboStep > 3) comboStep = 1; 
                
                animator.SetInteger("AttackType", comboStep);
                animator.SetTrigger("SwordAttack");
                lastAttackTime = Time.time;

                if (comboStep == 3)
                {
                    StartCoroutine(CinematicSlowMo(0.1f, 0.35f)); 
                    if(cameraFollowScript != null) cameraFollowScript.TriggerShake(0.4f, 0.25f); 
                }
                else
                {
                    if(cameraFollowScript != null) cameraFollowScript.TriggerShake(0.1f, 0.05f);
                }
            }
        }
    }

    private IEnumerator CinematicSlowMo(float targetTimeScale, float durationInRealtime)
    {
        Time.timeScale = targetTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; 

        yield return new WaitForSecondsRealtime(durationInRealtime);

        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
    }

    public void SetTalking(bool talking)
    {
        animator.SetBool("IsTalking", talking);
    }

    public void TakeDamage(int damageAmount)
    {
        animator.SetTrigger("TakeDamage");
        StartCoroutine(CinematicSlowMo(0.05f, 0.2f));
        if(cameraFollowScript != null) cameraFollowScript.TriggerShake(0.3f, 0.3f);
    }
}
