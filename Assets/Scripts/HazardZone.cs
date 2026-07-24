using UnityEngine;

/// <summary>
/// Attach this script to your Water and Lava GameObjects.
/// Make sure they have a Collider (either Trigger or normal).
/// When the player touches it, they will instantly die.
/// </summary>
public class HazardZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        KillPlayer(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        KillPlayer(collision.gameObject);
    }

    private void KillPlayer(GameObject obj)
    {
        if (obj.CompareTag("Player"))
        {
            PlayerController player = obj.GetComponent<PlayerController>();
            if (player != null)
            {
                Debug.Log("[HazardZone] Player touched " + gameObject.name + " and died instantly!");
                player.TakeDamage(9999); // 9999 damage means instant kill!
            }
        }
    }
}
