using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;

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

    [Header("Dodge Settings")]
    public float dodgeSpeed = 15f;
    public float dodgeDuration = 0.25f;
    public float dodgeCooldown = 1f;
    private bool isDodging = false;
    public bool isInvincible = false;
    private float lastDodgeTime = -10f;

    [Header("Audio")]
    public AudioSource playerAudio; 
    [Tooltip("Add reply audios here. e.g., 0 = Soul Shivraj, 1 = Physical Shivraj")]
    public AudioClip[] enemyReplies;
    
    [Header("Sound Effects")]
    public AudioClip[] footstepSounds;
    public AudioClip jumpSound;
    public AudioClip[] landingSounds;
    public AudioClip[] attackSounds;
    public AudioClip[] damageSounds;
    public AudioClip deathSound;
    private float footstepTimer = 0f;
    private bool wasGrounded = true;

    private CharacterController controller;
    private Animator animator;
    private Transform mainCameraTransform;
    private CameraFollow cameraFollowScript;
    
    private Vector3 moveDirection = Vector3.zero;
    private float verticalVelocity = 0f;
    private float currentLerpSpeed = 0f;
    private Renderer[] renderers;

    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public Slider healthSlider;
    private bool isDead = false;

    [Header("Interaction Settings")]
    public GameObject interactUI;

    public bool isFrozen = false; // Cinematic scene ke liye
    public bool isAttacking = false;
    private int comboStep = 0;
    private int slashComboStep = 0;
    private float lastAttackTime = 0f;

    [Header("Checkpoint System")]
    [HideInInspector] public static Vector3 lastCheckpointPosition = Vector3.zero;
    [HideInInspector] public static Quaternion lastCheckpointRotation = Quaternion.identity;
    [HideInInspector] public static bool hasCheckpoint = false;

    private float originalHeight = 2.0f;
    private Vector3 originalCenter = new Vector3(0, 1.0f, 0);

    void Start()
    {
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        
        if (interactUI != null)
        {
            interactUI.SetActive(false);
        }

        // Add AAA Voice Effect
        gameObject.AddComponent<AAAVoiceEffect>();

        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
            cameraFollowScript = Camera.main.GetComponent<CameraFollow>();
        }

        renderers = GetComponentsInChildren<Renderer>();
        
        // Checkpoint initialization & respawn setup
        if (hasCheckpoint)
        {
            if (controller != null) controller.enabled = false;
            transform.position = lastCheckpointPosition;
            transform.rotation = lastCheckpointRotation;
            if (controller != null) controller.enabled = true;
        }
        else
        {
            lastCheckpointPosition = transform.position;
            lastCheckpointRotation = transform.rotation;
            hasCheckpoint = true;
        }

        if (controller != null)
        {
            originalHeight = controller.height;
            originalCenter = controller.center;
        }

        if (playerAudio == null)
        {
            playerAudio = GetComponent<AudioSource>();
            if (playerAudio == null)
            {
                playerAudio = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    void Update()
    {
        if (isDead)
        {
            ApplyGravityOnly();
            return;
        }

        HandleAttacks(); 
        HandleMovement();
    }

    void ApplyGravityOnly()
    {
        if (controller != null && controller.enabled)
        {
            if (controller.isGrounded)
            {
                verticalVelocity = -2f; 
                // Disable controller so the capsule collision doesn't keep the mesh floating
                controller.enabled = false;
            }
            else
            {
                verticalVelocity -= gravity * Time.unscaledDeltaTime;
                controller.Move(new Vector3(0, verticalVelocity * Time.unscaledDeltaTime, 0));
            }
        }
    }

    void HandleMovement()
    {
        float horizontal = 0f;
        float vertical = 0f;
        bool isRunning = false;
        bool jumpPressed = false;

        if (Keyboard.current != null && !isAttacking && !isFrozen && !isDodging)
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

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && !isDodging && !isAttacking && !isFrozen && Time.time >= lastDodgeTime + dodgeCooldown)
        {
            StartCoroutine(DodgeRoutine(inputDirection));
        }

        if (isDodging)
        {
            // Stop other movement logic while dodging
            return;
        }

        if (isAttacking)
        {
            // Rotate towards camera when attacking
            if (mainCameraTransform != null)
            {
                float targetAngle = mainCameraTransform.eulerAngles.y;
                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.unscaledDeltaTime);
            }

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
            // Rotate towards camera when idle so it turns with the mouse
            if (mainCameraTransform != null && !isFrozen)
            {
                float targetAngle = mainCameraTransform.eulerAngles.y;
                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.unscaledDeltaTime);
            }

            moveDirection.x = 0;
            moveDirection.z = 0;
            animator.SetFloat("Speed", 0.0f, 0.1f, Time.unscaledDeltaTime);
        }

        if (controller.isGrounded)
        {
            if (!wasGrounded)
            {
                // Landed
                wasGrounded = true;
                if (playerAudio != null && landingSounds != null && landingSounds.Length > 0)
                {
                    playerAudio.PlayOneShot(landingSounds[Random.Range(0, landingSounds.Length)], 0.5f);
                }
            }

            verticalVelocity = -5f; 

            if (!isAttacking && jumpPressed)
            {
                verticalVelocity = jumpSpeed;
                animator.SetTrigger("Jump");
                
                if (playerAudio != null && jumpSound != null)
                {
                    playerAudio.PlayOneShot(jumpSound, 0.7f);
                }
                
                if(cameraFollowScript != null) cameraFollowScript.TriggerShake(0.1f, 0.05f);
            }
            
            // Footsteps logic
            if (moveDirection.magnitude > 0.1f && !isAttacking && !isDodging)
            {
                footstepTimer -= Time.unscaledDeltaTime;
                if (footstepTimer <= 0f)
                {
                    if (playerAudio != null && footstepSounds != null && footstepSounds.Length > 0)
                    {
                        playerAudio.PlayOneShot(footstepSounds[Random.Range(0, footstepSounds.Length)], 0.3f);
                    }
                    footstepTimer = isRunning ? 0.35f : 0.6f;
                }
            }
            else
            {
                footstepTimer = 0f;
            }
        }
        else
        {
            wasGrounded = false;
            verticalVelocity -= gravity * Time.unscaledDeltaTime;
        }

        moveDirection.y = verticalVelocity;
        controller.Move(moveDirection * Time.unscaledDeltaTime);
    }

    void HandleAttacks()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // Timer based approach: If attacked within the last 1.2 seconds, consider it an active attack.
        // This is 100% reliable even if Animator states have different names or no "Attack" tag.
        if (Time.time - lastAttackTime < 1.2f)
        {
            isAttacking = true;
        }
        else
        {
            isAttacking = false;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isFrozen)
        {
            // Combo logic: Wait at least 0.4 seconds before allowing the next combo input
            if (isAttacking && Time.time - lastAttackTime < 0.4f) return;

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
                
                if (playerAudio != null && attackSounds != null && attackSounds.Length > 0)
                {
                    playerAudio.PlayOneShot(attackSounds[Random.Range(0, attackSounds.Length)], 0.6f);
                }
                
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

                if (playerAudio != null && attackSounds != null && attackSounds.Length > 0)
                {
                    playerAudio.PlayOneShot(attackSounds[Random.Range(0, attackSounds.Length)], 0.5f);
                }

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

    private IEnumerator DodgeRoutine(Vector3 direction)
    {
        isDodging = true;
        isInvincible = true;
        lastDodgeTime = Time.time;

        if (direction.magnitude < 0.1f)
        {
            direction = -transform.forward;
        }
        else
        {
            direction = direction.normalized;
        }

        // Rotate towards dodge direction if we are not dodging backwards
        if (direction != -transform.forward && direction.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
        }

        float startTime = Time.time;
        while (Time.time < startTime + dodgeDuration)
        {
            controller.Move(direction * dodgeSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        isDodging = false;
        isInvincible = false;
    }

    private IEnumerator DamageFlashRoutine()
    {
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        propBlock.SetColor("_Color", Color.red);
        propBlock.SetColor("_BaseColor", Color.red);

        foreach (Renderer r in renderers)
        {
            if (r != null) r.SetPropertyBlock(propBlock);
        }

        yield return new WaitForSecondsRealtime(0.1f);

        foreach (Renderer r in renderers)
        {
            if (r != null) r.SetPropertyBlock(null);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead || isInvincible) return;

        // Apply the exact damage passed in (15 or 10)
        currentHealth -= damageAmount;

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else
        {
            animator.SetTrigger("TakeDamage");
            
            if (playerAudio != null && damageSounds != null && damageSounds.Length > 0)
            {
                playerAudio.PlayOneShot(damageSounds[Random.Range(0, damageSounds.Length)], 0.7f);
            }
            
            StartCoroutine(DamageFlashRoutine());
            StartCoroutine(CinematicSlowMo(0.05f, 0.2f));
            if(cameraFollowScript != null) cameraFollowScript.TriggerShake(0.3f, 0.3f);
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        
        if (playerAudio != null && deathSound != null)
        {
            playerAudio.PlayOneShot(deathSound, 1.0f);
        }
        
        animator.SetTrigger("Die");
        animator.Play("Die", 0, 0f);
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        if (controller != null)
        {
            controller.height = 0.2f;
            controller.center = new Vector3(0, 0.1f, 0);
        }

        // Wait for animation to play before freezing game and showing Game Over
        StartCoroutine(ShowGameOverAfterDelay(2f));
    }

    private IEnumerator ShowGameOverAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.PlayerDied();
        }
    }

    public void Respawn()
    {
        isDead = false;
        
        // Reset states that might get stuck
        isDodging = false;
        isInvincible = false;
        isFrozen = false;
        verticalVelocity = -2f;
        
        // Restore collider
        if (controller != null)
        {
            controller.enabled = false; // Disable temporarily to teleport
            controller.height = originalHeight;
            controller.center = originalCenter;
            
            transform.position = lastCheckpointPosition;
            transform.rotation = lastCheckpointRotation;
            
            controller.enabled = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        // Reset animator to default entry state (Idle)
        animator.Rebind();
        animator.Update(0f);
        animator.SetFloat("Speed", 0f);

        HealFull();
    }

    public void HealFull()
    {
        if (isDead) return;
        
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    public void ShowInteractUI(bool show)
    {
        if (interactUI != null)
        {
            interactUI.SetActive(show);
        }
    }
}
