using UnityEngine;
using UnityEngine.InputSystem;

public class HealthPickup : MonoBehaviour
{
    [Tooltip("Pickup hone ke baad object destroy hona chahiye ya nahi")]
    public bool destroyOnPickup = true;

    [Tooltip("Healthbox ke andar ki light, jo uthane par band ho jayegi")]
    public Light boxLight;

    private bool isPlayerInRange = false;
    private bool isCollected = false;
    private PlayerController player;

    private void Update()
    {
        if (isPlayerInRange && player != null && !isCollected)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                CollectPickup();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered healthbox trigger!");
            player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                isPlayerInRange = true;
                player.ShowInteractUI(true);
                Debug.Log("UI set to true");
            }
            else
            {
                Debug.Log("PlayerController script not found on the Player!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (player != null)
            {
                player.ShowInteractUI(false);
            }
            isPlayerInRange = false;
            player = null;
        }
    }

    private void CollectPickup()
    {
        isCollected = true;

        // Hide UI before destroying
        player.ShowInteractUI(false);

        // Heal the player completely
        player.HealFull();

        // Point light off kardo
        if (boxLight != null)
        {
            boxLight.enabled = false;
        }

        // Destroy the health box
        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }
}
