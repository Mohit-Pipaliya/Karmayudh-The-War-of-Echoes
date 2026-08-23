using System.Collections;
using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    [Header("Fall Settings")]
    [Tooltip("Kitne seconds baad block niche girega (2 se 3 second set kar sakte hain)")]
    public float fallDelay = 2.0f;
    
    [Tooltip("Block kitna niche jayega")]
    public float fallDistance = 5.0f;
    
    [Tooltip("Block ke niche girne ki speed")]
    public float fallSpeed = 10.0f;

    [Header("Return Settings")]
    [Tooltip("Kitne seconds baad block wapas upar aana shuru hoga")]
    public float returnDelay = 2.0f;
    
    [Tooltip("Block ke wapas aane ki speed")]
    public float returnSpeed = 5.0f;

    [Header("Collision Settings")]
    [Tooltip("Jis object ke touch hone par block girega, uska Tag yaha likhein. (Ya fir us object me CharacterController hona chahiye)")]
    public string targetTag = "Player";

    private Vector3 originalPosition;
    private bool isTriggered = false;

    void Start()
    {
        originalPosition = transform.position;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ye console me print karega ki block ko kisne chhua
        Debug.Log("Block ko isne touch kiya (Collision): " + collision.gameObject.name + " | Iska Tag hai: " + collision.gameObject.tag);
        CheckTrigger(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Block ko isne touch kiya (Trigger): " + other.gameObject.name + " | Iska Tag hai: " + other.gameObject.tag);
        CheckTrigger(other.gameObject);
    }

    void Update()
    {
        if (!isTriggered)
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Vector3 center = col.bounds.center + Vector3.up * (col.bounds.extents.y + 0.2f);
                Vector3 halfExtents = new Vector3(col.bounds.extents.x * 0.9f, 0.2f, col.bounds.extents.z * 0.9f);
                
                Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, transform.rotation);
                foreach (var hit in hitColliders)
                {
                    if (hit.gameObject != this.gameObject)
                    {
                        CheckTrigger(hit.gameObject);
                    }
                }
            }
        }
    }

    public void TriggerFall()
    {
        if (isTriggered) return;
        Debug.Log("<color=green>Block Girega!</color> Player ne direct isko Trigger kiya!");
        StartCoroutine(FallSequence());
    }

    private void CheckTrigger(GameObject obj)
    {
        if (isTriggered) return;

        string objName = obj.name.ToLower();

        // Player ko pehchanne ka tarika: Tag "Player" ho, CharacterController ho, YA phir naam me Shivraj ya Janam ho
        bool isPlayer = obj.CompareTag(targetTag) 
                        || obj.GetComponent<CharacterController>() != null 
                        || objName.Contains("shivraj") 
                        || objName.Contains("janam")
                        || objName.Contains("player");

        if (isPlayer)
        {
            Debug.Log("<color=green>Block Girega!</color> Player ne isko touch kiya: " + obj.name);
            StartCoroutine(FallSequence());
        }
    }

    private IEnumerator FallSequence()
    {
        isTriggered = true;

        // Niche girne ka intezaar karna
        yield return new WaitForSeconds(fallDelay);

        // Niche girna (fallDistance tak)
        Vector3 targetPosition = originalPosition - new Vector3(0, fallDistance, 0);
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, fallSpeed * Time.deltaTime);
            yield return null;
        }
        // Exactly target position par set karna
        transform.position = targetPosition;

        // Wapas aane se pehle thodi der rukna
        yield return new WaitForSeconds(returnDelay);

        // Wapas original position par aana
        while (Vector3.Distance(transform.position, originalPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPosition, returnSpeed * Time.deltaTime);
            yield return null;
        }
        // Exactly original position par set karna
        transform.position = originalPosition;

        // Fir se girne ke liye taiyaar
        isTriggered = false;
    }
}
