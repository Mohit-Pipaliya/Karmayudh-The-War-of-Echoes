using UnityEngine;

public class PlayerSwordDamage : MonoBehaviour
{
    private PlayerController player;
    
    // To prevent hitting the same enemy multiple times in one single swing
    private Collider lastHitEnemy;
    private float resetHitTimer = 0f;

    void Start()
    {
        player = GetComponentInParent<PlayerController>();
    }

    void Update()
    {
        if (resetHitTimer > 0)
        {
            resetHitTimer -= Time.deltaTime;
            if (resetHitTimer <= 0)
            {
                lastHitEnemy = null;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (player != null && player.isAttacking)
        {
            // Don't hit the same enemy 10 times in one frame
            if (other == lastHitEnemy) return;

            EnemyAI enemy1 = other.GetComponentInParent<EnemyAI>();
            EnemyAI2 enemy2 = other.GetComponentInParent<EnemyAI2>();
            EnemyAI3 enemy3 = other.GetComponentInParent<EnemyAI3>();

            if (enemy1 != null)
            {
                enemy1.TakeDamage(15); // Player damages Enemy 1 by 15%
                lastHitEnemy = other;
                resetHitTimer = 0.6f; 
                VFXManager.SpawnHitVFX(other.ClosestPoint(transform.position), true);
            }
            else if (enemy2 != null)
            {
                enemy2.TakeDamage(15); // Player damages Enemy 2 by 15%
                lastHitEnemy = other;
                resetHitTimer = 0.6f;
                VFXManager.SpawnHitVFX(other.ClosestPoint(transform.position), true);
            }
            else if (enemy3 != null)
            {
                enemy3.TakeDamage(10); // Player damages Enemy 3 by 10%
                lastHitEnemy = other;
                resetHitTimer = 0.6f;
                VFXManager.SpawnHitVFX(other.ClosestPoint(transform.position), true);
            }
        }
    }
}
