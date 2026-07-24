using UnityEngine;
using UnityEditor;

public class AAA_UpgradeTool : EditorWindow
{
    [MenuItem("Tools/Apply AAA Upgrade")]
    public static void ApplyUpgrade()
    {
        // Check if already exists
        GameObject existing = GameObject.Find("AAA_GameManager");
        if (existing != null)
        {
            Debug.Log("AAA GameManager already exists in the scene.");
            return;
        }

        // Create the manager
        GameObject manager = new GameObject("AAA_GameManager");
        
        // Add scripts
        manager.AddComponent<AAA_GraphicsManager>();
        manager.AddComponent<AAA_EnvironmentEffects>();

        Debug.Log("AAA Upgrade Applied! AAA_GameManager created with Graphics and Environment effects.");
    }
}
