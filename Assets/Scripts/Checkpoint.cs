using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is the player
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // Update the global respawn point to this checkpoint's position and rotation
                PlayerController.lastCheckpointPosition = transform.position;
                PlayerController.lastCheckpointRotation = transform.rotation;
                PlayerController.hasCheckpoint = true;
                
                Debug.Log("Checkpoint Saved!");
                
                // Optional: Disable this checkpoint so it doesn't keep saving if they walk back and forth
                GetComponent<Collider>().enabled = false; 
            }
        }
    }
}
