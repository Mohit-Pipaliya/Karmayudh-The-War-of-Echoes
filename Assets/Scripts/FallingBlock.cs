using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class FallingBlock : MonoBehaviour
{
    [Tooltip("Minimum time (in seconds) before the block falls after the player steps on it.")]
    public float minFallDelay = 2.0f;
    
    [Tooltip("Maximum time (in seconds) before the block falls after the player steps on it.")]
    public float maxFallDelay = 3.0f;

    [Tooltip("Optional: Time after falling before the block is destroyed. Set to 0 to disable.")]
    public float destroyDelay = 5.0f;

    private Rigidbody rb;
    private bool isFalling = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Block is completely static at the start
        rb.isKinematic = true;
        rb.useGravity = true; // Make sure gravity is enabled for when it falls
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the object stepping on the block is tagged as "Player"
        if (collision.gameObject.CompareTag("Player") && !isFalling)
        {
            StartCoroutine(FallCoroutine());
        }
    }

    // Alternatively, if you are using a trigger collider on top of the block to detect the player:
    /*
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isFalling)
        {
            StartCoroutine(FallCoroutine());
        }
    }
    */

    private IEnumerator FallCoroutine()
    {
        isFalling = true;

        // Pick a random time between 2 and 3 seconds
        float fallDelay = Random.Range(minFallDelay, maxFallDelay);
        
        // Wait for the delay
        yield return new WaitForSeconds(fallDelay);

        // Turn off isKinematic so gravity pulls the block down
        rb.isKinematic = false;

        // Destroy the block after it has fallen, to clean up the scene
        if (destroyDelay > 0)
        {
            Destroy(gameObject, destroyDelay);
        }
    }
}
