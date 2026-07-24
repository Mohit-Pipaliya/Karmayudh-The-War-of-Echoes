using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

// Ensure you have Universal Render Pipeline package installed.
// We use reflection or optional references if we wanted to be perfectly safe,
// but since URP is in the manifest, we can use it if we add the assembly reference or just rely on it.
// Actually, to avoid compiler errors in case URP is somehow removed, let's just write the steps for them in Hindi
// and provide a script that uses reflection or just basic Unity types, or I can just give them a robust script.

public class AAAStyleSetup : EditorWindow
{
    [MenuItem("Tools/Make Scene AAA Style")]
    public static void SetupAAAScene()
    {
        // 1. Setup Directional Light
        Light[] lights = FindObjectsOfType<Light>();
        Light sun = null;
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional)
            {
                sun = l;
                break;
            }
        }

        if (sun != null)
        {
            sun.color = new Color(1f, 0.95f, 0.9f); // Warm sun color
            sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft;
            sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;
            Debug.Log("AAA Setup: Directional Light Enhanced (Soft Shadows, High Res).");
        }

        // 2. Add Post-Processing Volume (Generic way for URP/HDRP)
        Volume volume = FindObjectOfType<Volume>();
        if (volume == null)
        {
            GameObject volumeObject = new GameObject("AAA_Global_PostProcessing");
            volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            Debug.Log("AAA Setup: Created Global Volume for Post Processing.");
        }

        VolumeProfile profile = volume.sharedProfile;
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;
            Debug.Log("AAA Setup: Please manually add overrides (Tonemapping, Bloom, Vignette) to the Volume Profile!");
        }

        // 3. Set Lighting Environment
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientEquatorColor = new Color(0.6f, 0.6f, 0.6f);
        RenderSettings.ambientGroundColor = new Color(0.3f, 0.3f, 0.3f);
        RenderSettings.ambientSkyColor = new Color(0.8f, 0.9f, 1.0f);
        
        // 4. Fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.002f;
        RenderSettings.fogColor = new Color(0.7f, 0.8f, 0.9f);

        Debug.Log("AAA Setup Complete! \nNext steps:\n1. Ensure Main Camera has Post Processing checked.\n2. Add Bloom, ACES Tonemapping, and Vignette to your Global Volume.\n3. Bake your Lighting!");
        
        #if UNITY_EDITOR
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        #endif
    }
}
