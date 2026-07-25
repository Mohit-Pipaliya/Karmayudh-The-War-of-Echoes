using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class EnemyAI2 : MonoBehaviour
{
    [Header("Target & Arena")]
    public Transform player; 
    public GameObject arenaWalls; 

    [Header("Cinematic Audio")]
    [Tooltip("Player jo audio bolega (ye clip player ke AudioSource me chalegi)")]
    public AudioClip playerDialogueClip;

    [Header("Stats")]
    public float triggerRange = 15f; 
    public float cutsceneRange = 8f; 
    public float attackRange = 1.5f; 
    public float chaseSpeed = 2.5f; // Walk speed instead of run speed
    public float rotationSpeed = 10f;
    public float gravity = 25f;
    public int maxHealth = 100;

    [Header("Combat Settings")]
    public float attackCooldown = 1.5f;

    private int currentHealth;
    private EnemyState currentState = EnemyState.Idle;
    
    private CharacterController controller;
    private Animator animator;
    private AudioSource audioSource;
    private PlayerController playerControllerScript;
    
    private float verticalVelocity = 0f;
    private bool isAttackCoolingDown = false;
    private GameObject activeLightningArena;
    private EnemyHealthBar healthBar;
    private Renderer[] renderers;

    [Header("Sound Effects")]
    public AudioClip[] footstepSounds;
    public AudioClip[] attackSounds;
    public AudioClip[] damageSounds;
    public AudioClip deathSound;
    public AudioClip growlSound;
    private float footstepTimer = 0f;
    private float growlTimer = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        currentHealth = maxHealth;
        
        // Add AAA Voice Effect
        gameObject.AddComponent<AAAVoiceEffect>();
        
        // Add Health Bar (Red Glowing for Enemy 2)
        healthBar = gameObject.AddComponent<EnemyHealthBar>();
        healthBar.Initialize(new Color(2.5f, 0f, 0f, 1f)); // HDR Red
        
        if (arenaWalls != null)
            arenaWalls.SetActive(false); 

        if (player == null && GameObject.FindGameObjectWithTag("Player") != null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        if (player != null)
        {
            playerControllerScript = player.GetComponent<PlayerController>();
        }

        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        if (controller != null && controller.enabled)
        {
            ApplyGravity();
        }

        if (currentState == EnemyState.Dead)
            return; 

        // Random growl sound logic
        if (audioSource != null && growlSound != null)
        {
            growlTimer -= Time.deltaTime;
            if (growlTimer <= 0)
            {
                audioSource.PlayOneShot(growlSound, 0.4f);
                growlTimer = Random.Range(5f, 15f);
            }
        }
        
        // Footstep logic
        if (currentState == EnemyState.Chasing && footstepSounds != null && footstepSounds.Length > 0)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0)
            {
                audioSource.PlayOneShot(footstepSounds[Random.Range(0, footstepSounds.Length)], 0.3f);
                footstepTimer = 0.5f; // Hardcode time between steps while running
            }
        }

        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle(distanceToPlayer);
                break;
            case EnemyState.Talking:
                HandleTalking();
                break;
            case EnemyState.PlayerTalking:
                HandlePlayerTalking();
                break;
            case EnemyState.Chasing:
                HandleChasing(distanceToPlayer);
                break;
            case EnemyState.Attacking:
                HandleAttacking(distanceToPlayer);
                break;
        }
    }

    void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        
        controller.Move(new Vector3(0, verticalVelocity * Time.deltaTime, 0));
    }

    void HandleIdle(float distance)
    {
        animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);

        // Agar player yellow circle (trigger) me hai, toh enemy usay bas ghoorega (Face karega)
        if (distance <= triggerRange && distance > cutsceneRange)
        {
            FacePlayer();
        }

        // Jaise hi player Cutscene Range me aayega, tab Cinematic aur Audio chalu hoga!
        if (distance <= cutsceneRange)
        {
            if (WorldEnvironmentManager.Instance != null) WorldEnvironmentManager.Instance.AddBGMSuppression();
            currentState = EnemyState.Talking;
            
            // --- CUTSCENE SHURU ---
            if (playerControllerScript != null)
            {
                playerControllerScript.isFrozen = true; // Player hil nahi payega
            }
            
            // Dono ko ek dusre ke samne face karwao
            if (player != null)
            {
                Vector3 lookDir = (transform.position - player.position).normalized;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    player.rotation = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.LookRotation(-lookDir);
                }
            }
            
            if (Camera.main != null)
            {
                CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
                if (cam != null) cam.StartCinematic(transform); // Camera side me chala jayega
            }

            if (audioSource.clip != null)
            {
                audioSource.pitch = 1.4f; // Speed up audio more (40%)
                audioSource.spatialBlend = 0f; // Make 2D so it's loud and clear
                audioSource.volume = 1f;
                audioSource.Play();
            }
            animator.SetBool("IsTalking", true); 
        }
    }

    void HandleTalking()
    {
        // Naya Logic: Baat karte waqt Enemy hamesha Player ki taraf ghoomega (Face karega)
        FacePlayer();

        if (!audioSource.isPlaying)
        {
            animator.SetBool("IsTalking", false);
            
            // Jab enemy chup ho jaye, toh player ki baari
            currentState = EnemyState.PlayerTalking;

            // Player ko enemy ki taraf face karwao (Jab player reply dega)
            Vector3 lookDir = (transform.position - player.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                player.rotation = Quaternion.LookRotation(lookDir);
            }

            if (playerControllerScript != null && playerControllerScript.playerAudio != null && playerDialogueClip != null)
            {
                playerControllerScript.playerAudio.clip = playerDialogueClip;
                playerControllerScript.playerAudio.pitch = 1.4f; // Speed up audio more (40%)
                playerControllerScript.playerAudio.spatialBlend = 0f; // Make 2D to fix low volume issue
                playerControllerScript.playerAudio.volume = 1f; // Max volume
                playerControllerScript.playerAudio.Play();
                
                // Player ko bolne wali animation chalu karne ka ishara
                playerControllerScript.SetTalking(true);
            }
            else
            {
                // Agar player me audio nahi hai, toh direct chase shuru karo
                EndCutscene();
            }
        }
    }

    void HandlePlayerTalking()
    {
        FacePlayer(); // Enemy abhi bhi ghura karega

        // Jab player ka audio bolna band ho jaye
        if (playerControllerScript == null || playerControllerScript.playerAudio == null || !playerControllerScript.playerAudio.isPlaying)
        {
            if (playerControllerScript != null)
            {
                // Player ki bolne wali animation band karo
                playerControllerScript.SetTalking(false);
            }
            EndCutscene();
        }
    }

    void EndCutscene()
    {
        if (WorldEnvironmentManager.Instance != null) WorldEnvironmentManager.Instance.RemoveBGMSuppression();
        currentState = EnemyState.Chasing;
        
        // --- CUTSCENE KHATAM ---
        if (playerControllerScript != null)
        {
            playerControllerScript.isFrozen = false; // Player ab lad sakta hai
        }

        if (Camera.main != null)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null) cam.StopCinematic(); // Camera wapas apni jagah
        }

        if (activeLightningArena == null)
        {
            activeLightningArena = new GameObject("LightningArena");
            // NAYA LOGIC: Arena ring ko exactly enemy ke trigger area (yellow circle) par banana hai
            Vector3 center = transform.position; // Enemy ka center
            center.y = transform.position.y;
            activeLightningArena.transform.position = center;
            
            LightningArena arenaScript = activeLightningArena.AddComponent<LightningArena>();
            
            // Arena ka size triggerRange (15f) jitna set kar diya gaya hai
            arenaScript.Initialize(triggerRange);
        }
    }

    void HandleChasing(float distance)
    {
        if (distance <= attackRange)
        {
            currentState = EnemyState.Attacking;
            return;
        }

        FacePlayer();

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        controller.Move(direction * chaseSpeed * Time.deltaTime);
        
        // 1.0f se 'Run' animation chalega jo zyada fast lagta hai
        animator.SetFloat("Speed", 1f, 0.1f, Time.deltaTime);
    }

    void HandleAttacking(float distance)
    {
        animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime); 
        FacePlayer();

        if (distance > attackRange && !isAttackCoolingDown)
        {
            currentState = EnemyState.Chasing;
        }
        else if (!isAttackCoolingDown)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    void FacePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; 
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttackCoolingDown = true;

        // Pehla Attack
        int attackChoice = Random.Range(0, 2); 
        
        if (attackChoice == 0)
        {
            int slashType = Random.Range(1, 3);
            animator.SetInteger("AttackType", slashType);
            animator.SetTrigger("SlashAttack");
        }
        else
        {
            int attackType = Random.Range(1, 4);
            animator.SetInteger("AttackType", attackType);
            animator.SetTrigger("SwordAttack");
        }

        if (audioSource != null && attackSounds != null && attackSounds.Length > 0)
        {
            audioSource.PlayOneShot(attackSounds[Random.Range(0, attackSounds.Length)], 0.6f);
        }

        // Ek extra attack (Combo)
        yield return new WaitForSeconds(0.6f);

        int attackChoice2 = Random.Range(0, 2); 
        if (attackChoice2 == 0)
        {
            animator.SetInteger("AttackType", Random.Range(1, 3));
            animator.SetTrigger("SlashAttack");
        }
        else
        {
            animator.SetInteger("AttackType", Random.Range(1, 4));
            animator.SetTrigger("SwordAttack");
        }

        if (audioSource != null && attackSounds != null && attackSounds.Length > 0)
        {
            audioSource.PlayOneShot(attackSounds[Random.Range(0, attackSounds.Length)], 0.6f);
        }

        yield return new WaitForSeconds(attackCooldown);

        isAttackCoolingDown = false;
    }

    public void TakeDamage(int damage)
    {
        if (currentState == EnemyState.Dead) return;

        currentHealth -= damage;
        
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
        
        StartCoroutine(DamageFlashRoutine());

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(HitPause());
            animator.SetTrigger("TakeDamage");
            if (audioSource != null && damageSounds != null && damageSounds.Length > 0)
            {
                audioSource.PlayOneShot(damageSounds[Random.Range(0, damageSounds.Length)], 0.7f);
            }
        }
    }

    public bool IsAttacking()
    {
        return currentState == EnemyState.Attacking;
    }

    IEnumerator HitPause()
    {
        animator.speed = 0.1f;
        yield return new WaitForSeconds(0.1f);
        animator.speed = 1f;
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

    void Die()
    {
        currentState = EnemyState.Dead;
        animator.speed = 1f; // Force normal speed
        
        // Reset any pending triggers that might interfere with the Die transition
        animator.ResetTrigger("TakeDamage");
        animator.ResetTrigger("SlashAttack");
        animator.ResetTrigger("SwordAttack");
        
        // Use SetTrigger, and also try Play as a forceful fallback
        animator.SetTrigger("Die"); 
        try { animator.Play("Die", 0, 0f); } catch {}
        
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound, 1.0f);
        }

        if (healthBar != null)
        {
            healthBar.HideBar();
        }

        if (arenaWalls != null)
            arenaWalls.SetActive(false);
            
        if (activeLightningArena != null)
        {
            Destroy(activeLightningArena);
        }
            
        StartCoroutine(DisableControllerAfterDeath());
    }

    IEnumerator DisableControllerAfterDeath()
    {
        yield return new WaitForSeconds(3f);
        if (controller != null)
            controller.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
