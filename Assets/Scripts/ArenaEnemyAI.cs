using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ArenaEnemyAI : MonoBehaviour
{
    public enum EnemyState { Patrol, Chase, Attack, Dead }
    [Header("Current State")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Patrol Settings")]
    [Tooltip("Point A, Point B, or more points for patrolling")]
    public Transform[] patrolPoints;
    private int currentPatrolIndex;
    public float patrolSpeed = 2f;

    [Header("Chase Settings")]
    public float triggerRadius = 15f;
    public float chaseSpeed = 5f;
    private Transform playerTransform;
    
    [Header("Attack Settings")]
    public float attackRadius = 3f;
    public float attackCooldown = 2f;
    private float nextAttackTime = 0f;
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Health Bar Settings")]
    [Tooltip("Health bar kitna bada (radius) hona chahiye")]
    public float healthBarRadius = 0.45f; 
    [Tooltip("Health bar ke dashes ki motai aur lambaai")]
    public Vector2 healthBarSegmentSize = new Vector2(0.15f, 0.04f);

    // Arena will be generated from script now
    private GameObject activeArenaWall;
    private bool isArenaActive = false;
    
    [Header("Effects & Feedback")]
    public AudioClip damageSound;
    public AudioClip deathSound;
    
    private NavMeshAgent agent;
    private Animator animator;
    private AudioSource audioSource;
    private EnemyHealthBar healthBar;
    private bool isAttacking = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if(audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        currentHealth = maxHealth;
        
        healthBar = gameObject.AddComponent<EnemyHealthBar>();
        
        // Skeleton (Arena Enemy) ke hisaab se health bar ko aur bhi chhota kiya gaya hai
        healthBar.radius = healthBarRadius; 
        healthBar.segmentSize = healthBarSegmentSize; 
        
        healthBar.Initialize(new Color(0.8f, 0f, 0f, 1f)); // Glowing Red for Arena Enemy
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        if (patrolPoints != null && patrolPoints.Length > 0 && agent.isOnNavMesh)
        {
            agent.SetDestination(patrolPoints[0].position);
        }
    }

    void Update()
    {
        if (currentState == EnemyState.Dead || playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol(distanceToPlayer);
                break;
            case EnemyState.Chase:
                UpdateChase(distanceToPlayer);
                break;
            case EnemyState.Attack:
                UpdateAttack(distanceToPlayer);
                break;
        }
    }

    void UpdatePatrol(float distanceToPlayer)
    {
        if (agent.isOnNavMesh)
        {
            agent.speed = patrolSpeed;
            agent.isStopped = false;
            
            // Animator parameters for Walk
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsRunning", false);

            // Agar pehle se koi rasta set nahi hai, toh turant set karo
            if (!agent.hasPath && patrolPoints != null && patrolPoints.Length > 0)
            {
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
            // Agar pahunch gaya hai, toh agla point chuno
            else if (patrolPoints != null && patrolPoints.Length > 0 && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
        }
        else
        {
            Debug.LogWarning("Skeleton NavMesh (Zameen) par nahi hai! Isko thoda neeche karo.");
        }

        if (distanceToPlayer <= triggerRadius)
        {
            ActivateArena();
            currentState = EnemyState.Chase;
        }
    }

    void UpdateChase(float distanceToPlayer)
    {
        if (agent.isOnNavMesh)
        {
            agent.speed = chaseSpeed;
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
            
            // Animator parameters for Run/Chase
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", true);
        }
        
        if (distanceToPlayer <= attackRadius)
        {
            currentState = EnemyState.Attack;
        }
    }

    void UpdateAttack(float distanceToPlayer)
    {
        if (isAttacking) return; 

        if (agent.isOnNavMesh) agent.isStopped = true;
        
        // Stop walking/running when attacking
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsRunning", false);

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        if(direction != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 5f);
        }

        if (Time.time >= nextAttackTime)
        {
            StartCoroutine(PerformAttack());
        }
        
        if (distanceToPlayer > attackRadius && !isAttacking)
        {
            agent.isStopped = false;
            currentState = EnemyState.Chase;
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        
        int attackType = Random.Range(1, 4);
        animator.SetTrigger("Attack" + attackType);
        
        yield return new WaitForSeconds(1.5f); 
        
        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    // GENERATING ARENA DYNAMICALLY
    void ActivateArena()
    {
        if (!isArenaActive)
        {
            isArenaActive = true;
            
            // Create a parent object for the Arena
            activeArenaWall = new GameObject("Dynamic_AAA_Circular_Arena");
            activeArenaWall.transform.position = transform.position; // Center it where the enemy spotted the player

            // Using Sprites/Default because it works natively with Transparency and Color in Built-in, URP, and HDRP without pink errors!
            Material arenaMat = new Material(Shader.Find("Sprites/Default"));
            arenaMat.color = new Color(1f, 0f, 0f, 0.4f); // Semi-transparent glowing Red

            float wallThickness = 0.5f;
            float wallHeight = 15f;
            int segments = 36; // 36 cubes to make a smooth circle
            float angleStep = 360f / segments;
            float segmentWidth = (2 * Mathf.PI * triggerRadius) / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                
                // Position around the circle
                float x = Mathf.Cos(angle) * triggerRadius;
                float z = Mathf.Sin(angle) * triggerRadius;
                Vector3 pos = new Vector3(x, wallHeight / 2, z);

                // Rotation facing outward from center
                Quaternion rot = Quaternion.LookRotation(new Vector3(x, 0, z).normalized);

                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "WallSegment_" + i;
                wall.transform.SetParent(activeArenaWall.transform);
                wall.transform.localPosition = pos;
                wall.transform.localRotation = rot;
                
                // Scale: X = width to touch next segment, Y = height, Z = thickness
                wall.transform.localScale = new Vector3(segmentWidth + 0.5f, wallHeight, wallThickness);
                
                wall.GetComponent<MeshRenderer>().material = arenaMat;
            }
            
            Debug.Log("Dynamic Circular Arena Generated! Player is trapped.");
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (currentState == EnemyState.Dead) return;

        currentHealth -= damageAmount;
        
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
        
        animator.SetTrigger("Damage");
        
        if (damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        currentState = EnemyState.Dead;
        agent.isStopped = true;
        agent.enabled = false;
        GetComponent<Collider>().enabled = false; 
        
        animator.SetTrigger("Die");
        
        if (deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        if (activeArenaWall != null)
        {
            Destroy(activeArenaWall); // Destroy arena so player can leave
            isArenaActive = false;
        }

        if (healthBar != null)
        {
            healthBar.HideBar();
        }

        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(2.5f); 

        // GENERATING FOG DYNAMICALLY
        GameObject fogObj = new GameObject("Dynamic_DeathFog");
        fogObj.transform.position = transform.position + Vector3.up * 1f;
        
        ParticleSystem ps = fogObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.duration = 2f;
        main.startLifetime = 3f;
        main.startSpeed = 1f;
        main.startSize = 3f;
        main.startColor = new Color(0.3f, 0.3f, 0.3f, 0.8f); // Grey fog
        main.maxParticles = 50;
        main.loop = false;
        
        var emission = ps.emission;
        emission.rateOverTime = 30f;
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 2f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.black, 0.0f), new GradientColorKey(Color.gray, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 0.8f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 3f);

        // By default Unity adds a ParticleSystemRenderer with Default-Material.
        // We will make it render softly if possible
        ParticleSystemRenderer renderer = fogObj.GetComponent<ParticleSystemRenderer>();
        if(renderer != null)
        {
            // Just assigning standard sorting
            renderer.sortingOrder = 10;
        }

        Destroy(fogObj, 5f); // Destroy fog after 5 seconds

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
