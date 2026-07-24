using UnityEngine;
using System.Collections;

public class GuideCheckpoint : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Drag the GuideEnemyManager here. If empty, it will try to find it in the scene.")]
    public GuideEnemyManager guideManager;
    
    [Tooltip("The exact position and rotation where the enemy should appear for this checkpoint. If empty, it uses this trigger's position.")]
    public Transform guideSpawnPoint;

    [Tooltip("How many seconds the enemy will be visible.")]
    public float showDuration = 2.0f;

    [Header("Final Checkpoint")]
    [Tooltip("Tick this if this is the LAST checkpoint. The enemy will return to its original place and turn back into a real enemy.")]
    public bool isLastCheckpoint = false;

    private bool hasTriggered = false;

    private void Start()
    {
        if (guideManager == null)
        {
            guideManager = FindObjectOfType<GuideEnemyManager>();
        }

        if (guideSpawnPoint == null)
        {
            guideSpawnPoint = this.transform;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;

            // Player ko enemy ki taraf face karwana
            StartCoroutine(FacePlayerToEnemy(other.transform));

            if (guideManager != null)
            {
                if (isLastCheckpoint)
                {
                    guideManager.ShowFinalGuide(guideSpawnPoint.position, guideSpawnPoint.rotation, showDuration);
                }
                else
                {
                    guideManager.ShowGuide(guideSpawnPoint.position, guideSpawnPoint.rotation, showDuration);
                }
            }
        }
    }

    private IEnumerator FacePlayerToEnemy(Transform player)
    {
        // Player smoothly enemy ki taraf ghumega 0.5 seconds mein
        Vector3 direction = (guideSpawnPoint.position - player.position).normalized;
        direction.y = 0; // Taki player upar ya neeche na dekhe
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float time = 0;
            Quaternion startRotation = player.rotation;
            
            while (time < 0.5f)
            {
                player.rotation = Quaternion.Slerp(startRotation, targetRotation, time / 0.5f);
                time += Time.deltaTime;
                yield return null;
            }
            player.rotation = targetRotation;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isLastCheckpoint ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0, 0.3f);
        Gizmos.DrawCube(transform.position, GetComponent<BoxCollider>() != null ? GetComponent<BoxCollider>().size : Vector3.one);
        
        if (guideSpawnPoint != null)
        {
            Gizmos.color = isLastCheckpoint ? Color.yellow : Color.red;
            Gizmos.DrawSphere(guideSpawnPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, guideSpawnPoint.position);
        }
    }
}
