using UnityEngine;

public class JanamTrigger : MonoBehaviour
{
    [Header("Janam Settings")]
    [Tooltip("The text that will be shown when the player enters this trigger (e.g., 'Janam 2' or 'Janam 3')")]
    public string janamNameToSet = "Janam 2";

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateJanamText(janamNameToSet);
                hasTriggered = true; // So it only triggers once
            }
        }
    }
}
