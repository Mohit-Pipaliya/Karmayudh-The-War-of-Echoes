using UnityEngine;

public class EnemyWeaponDamage : MonoBehaviour
{
    [Tooltip("Set this to 15 for Enemy 1 and 2. Set to 10 for Enemy 3.")]
    public int damageAmount = 15; 
    
    private Animator enemyAnimator;
    private Collider lastHitPlayer;
    private float resetHitTimer = 0f;

    void Start()
    {
        enemyAnimator = GetComponentInParent<Animator>();
    }

    void Update()
    {
        if (resetHitTimer > 0)
        {
            resetHitTimer -= Time.deltaTime;
            if (resetHitTimer <= 0)
            {
                lastHitPlayer = null;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        bool isAttacking = false;
        if (enemyAnimator != null)
        {
            AnimatorStateInfo stateInfo = enemyAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsTag("Attack") || stateInfo.IsName("Attack") || stateInfo.IsName("SwordAttack") || stateInfo.IsName("SlashAttack"))
            {
                isAttacking = true;
            }
        }

        // Fallback for EnemyAI3 (or others) where animation state names might not match exactly
        if (!isAttacking)
        {
            EnemyAI3 ai3 = GetComponentInParent<EnemyAI3>();
            if (ai3 != null && ai3.IsAttacking())
            {
                isAttacking = true;
            }
            else
            {
                EnemyAI2 ai2 = GetComponentInParent<EnemyAI2>();
                if (ai2 != null && ai2.IsAttacking()) isAttacking = true;
                else
                {
                    EnemyAI ai1 = GetComponentInParent<EnemyAI>();
                    if (ai1 != null && ai1.IsAttacking()) isAttacking = true;
                }
            }
        }

        if (isAttacking)
        {
            if (other == lastHitPlayer) return;

            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damageAmount); // Enemy damages player by specific amount
                lastHitPlayer = other;
                resetHitTimer = 1.0f; // Prevent multi-hit from the same sword swing
                VFXManager.SpawnHitVFX(other.ClosestPoint(transform.position), true);
            }
        }
    }
}
