using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// This script dynamically adds and configures a AAA Quality URP Post-Processing Volume.
/// It applies Hollywood-style ACES Tonemapping, Bloom, Vignette, and Color Grading.
/// </summary>
[RequireComponent(typeof(Volume))]
public class AAAPostProcessingSetup : MonoBehaviour
{
    private Volume globalVolume;

    void Start()
    {
        // 1. Get or Add Volume Component
        globalVolume = GetComponent<Volume>();
        globalVolume.isGlobal = true;

        // 2. Create a new Profile
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave; // Prevents Unity Editor Inspector crash
        globalVolume.profile = profile;

        // 3. Add Tonemapping (THE MOST IMPORTANT SETTING FOR AAA LOOK)
        Tonemapping tonemapping = profile.Add<Tonemapping>();
        if (tonemapping != null)
        {
            tonemapping.mode.Override(TonemappingMode.ACES);
        }

        // 4. Add Bloom (Makes magic, swords, and sun glow softly)
        Bloom bloom = profile.Add<Bloom>();
        if (bloom != null)
        {
            bloom.intensity.Override(1.5f);
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.7f); // Wide, soft bloom instead of sharp glow
            bloom.tint.Override(Color.white);
        }

        // 5. Add Vignette (Darkens screen corners for cinematic focus)
        Vignette vignette = profile.Add<Vignette>();
        if (vignette != null)
        {
            vignette.intensity.Override(0.35f);
            vignette.smoothness.Override(0.8f);
            vignette.color.Override(Color.black);
            vignette.rounded.Override(false);
        }

        // 6. Add Color Adjustments (Contrast & Saturation)
        ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>();
        if (colorAdjustments != null)
        {
            colorAdjustments.contrast.Override(20f);   // Pop the colors
            colorAdjustments.saturation.Override(5f); // Slightly saturated
        }
        
        // Ensure Main Camera has Post Processing enabled
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            var camData = mainCam.GetComponent<UniversalAdditionalCameraData>();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
                // FXAA is 3x faster than SMAA, providing massive FPS boost while keeping edges smooth
                camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                camData.antialiasingQuality = AntialiasingQuality.Low;
            }
        }
    }

    void OnDestroy()
    {
        // CRITICAL: Clean up the dynamically created ScriptableObject so the Unity Editor Inspector doesn't crash after stopping Play Mode!
        if (globalVolume != null && globalVolume.profile != null)
        {
            Destroy(globalVolume.profile);
        }
    }
}
