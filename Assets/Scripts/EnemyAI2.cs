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

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        currentHealth = maxHealth;
        
        // Add AAA Voice Effect
        gameObject.AddComponent<AAAVoiceEffect>();
        
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
    }

    void Update()
    {
        if (currentState == EnemyState.Dead)
            return; 

        ApplyGravity();

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
            currentState = EnemyState.Talking;
            
            // --- CUTSCENE SHURU ---
            if (playerControllerScript != null)
            {
                playerControllerScript.isFrozen = true; // Player hil nahi payega
            }
            
            if (Camera.main != null)
            {
                CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
                if (cam != null) cam.StartCinematic(transform); // Camera side me chala jayega
            }

            if (audioSource.clip != null)
            {
                audioSource.pitch = 1.2f; // Speed up audio by 20%
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

            if (playerControllerScript != null && playerControllerScript.playerAudio != null && playerDialogueClip != null)
            {
                playerControllerScript.playerAudio.clip = playerDialogueClip;
                playerControllerScript.playerAudio.pitch = 1.2f; // Speed up audio by 20%
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
            Vector3 center = (transform.position + player.position) / 2f;
            center.y = transform.position.y;
            activeLightningArena.transform.position = center;
            
            LightningArena arenaScript = activeLightningArena.AddComponent<LightningArena>();
            float dist = Vector3.Distance(transform.position, player.position);
            arenaScript.Initialize(Mathf.Max(dist, 12f));
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

        yield return new WaitForSeconds(attackCooldown);

        isAttackCoolingDown = false;
    }

    public void TakeDamage(int damage)
    {
        if (currentState == EnemyState.Dead) return;

        currentHealth -= damage;
        
        StartCoroutine(HitPause());

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            animator.SetTrigger("TakeDamage");
        }
    }

    IEnumerator HitPause()
    {
        animator.speed = 0.1f;
        yield return new WaitForSeconds(0.1f);
        animator.speed = 1f;
    }

    void Die()
    {
        currentState = EnemyState.Dead;
        animator.SetTrigger("Die"); 

        if (arenaWalls != null)
            arenaWalls.SetActive(false);
            
        if (activeLightningArena != null)
        {
            Destroy(activeLightningArena);
        }
            
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
