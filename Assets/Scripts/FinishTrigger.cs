using UnityEngine;

public class FinishTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Jab bhi koi object is trigger me aaye, check karo ki kya wo Player hai
        if (other.CompareTag("Player"))
        {
            // Scene me UIManager dhundo
            UIManager uiManager = FindObjectOfType<UIManager>();
            
            if (uiManager != null)
            {
                // UIManager ko batao ki game khatam ho gaya
                uiManager.GameFinished();
                
                // Trigger ko disable kar do taaki ye baar-baar call na ho
                GetComponent<Collider>().enabled = false;
            }
            else
            {
                Debug.LogError("FinishTrigger: Scene me UIManager nahi mila!");
            }
        }
    }
}
