using UnityEngine;

/// <summary>
/// Unlocks FPS to maximum, disables VSync, and applies smart graphical downgrades 
/// to boost FPS without ruining the AAA look. Attach this to a global manager.
/// </summary>
public class FPSBooster : MonoBehaviour
{
    [Header("Target FPS Settings")]
    public int targetFPS = 120;
    
    void Start()
    {
        ApplyOptimizations();
    }

    public void ApplyOptimizations()
    {
        Debug.Log("[FPS Booster] Applying AAA Optimizations...");

        // 1. Disable VSync (CRITICAL for getting past 60 FPS)
        QualitySettings.vSyncCount = 0;

        // 2. Unlock Frame Rate
        Application.targetFrameRate = targetFPS;

        // 3. Optimize Physics (Reduces CPU load heavily in complex scenes)
        // Default is 0.02 (50fps physics). We change it to slightly faster step.
        // Actually, for action games 0.02 is fine, but we can lower solver iterations.
        Physics.defaultSolverIterations = 4; // Default is 6
        Physics.defaultSolverVelocityIterations = 1; // Default is 1

        // 4. Optimize Shadows (Massive FPS gain)
        // Keep shadows but reduce distance and cascades so GPU breathes
        if (QualitySettings.shadowCascades > 2)
        {
            QualitySettings.shadowCascades = 2; // Medium quality cascades
        }
        QualitySettings.shadowDistance = 75f; // Beyond 75m, no shadows. (Fog covers it anyway!)

        // 5. Optimize Pixel Lights
        QualitySettings.pixelLightCount = 2; // Max 2 per-pixel lights
        
        Debug.Log($"[FPS Booster] Target FPS set to {Application.targetFrameRate}. VSync is OFF.");
    }
}
