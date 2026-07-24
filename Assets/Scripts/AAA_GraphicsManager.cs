using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AAA_GraphicsManager : MonoBehaviour
{
    private Volume globalVolume;
    private VolumeProfile profile;

    void Start()
    {
        SetupPostProcessing();
    }

    void SetupPostProcessing()
    {
        // Create a new GameObject for the Global Volume
        GameObject volumeObj = new GameObject("AAA_GlobalVolume");
        globalVolume = volumeObj.AddComponent<Volume>();
        globalVolume.isGlobal = true;
        globalVolume.priority = 100; // High priority to override defaults

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        globalVolume.profile = profile;

        // 1. Tonemapping (ACES is industry standard for AAA realistic colors)
        if (!profile.TryGet(out Tonemapping tonemapping))
        {
            tonemapping = profile.Add<Tonemapping>();
        }
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);

        // 2. Bloom (Soft glow for lights/sky)
        if (!profile.TryGet(out Bloom bloom))
        {
            bloom = profile.Add<Bloom>();
        }
        bloom.active = true;
        bloom.intensity.Override(0.8f);
        bloom.threshold.Override(1.0f);
        bloom.scatter.Override(0.7f); // Wide, realistic glow
        bloom.tint.Override(Color.white);

        // 3. Vignette (Darkens edges to focus on center)
        if (!profile.TryGet(out Vignette vignette))
        {
            vignette = profile.Add<Vignette>();
        }
        vignette.active = true;
        vignette.intensity.Override(0.35f);
        vignette.smoothness.Override(0.8f);
        vignette.color.Override(Color.black);

        // 4. Color Adjustments (Contrast and Saturation boost)
        if (!profile.TryGet(out ColorAdjustments colorAdjust))
        {
            colorAdjust = profile.Add<ColorAdjustments>();
        }
        colorAdjust.active = true;
        colorAdjust.contrast.Override(15f); // Punchy shadows
        colorAdjust.saturation.Override(10f); // Vibrant colors
        colorAdjust.postExposure.Override(0.2f); // Slightly brighter

        // 5. Split Toning (Cinematic movie look - Teal & Orange)
        if (!profile.TryGet(out SplitToning splitToning))
        {
            splitToning = profile.Add<SplitToning>();
        }
        splitToning.active = true;
        splitToning.shadows.Override(new Color(0.1f, 0.15f, 0.2f)); // Cool shadows (Teal-ish)
        splitToning.highlights.Override(new Color(1.0f, 0.9f, 0.8f)); // Warm highlights (Orange-ish)
        splitToning.balance.Override(0f);

        Debug.Log("AAA Post-Processing Setup Complete!");
    }
}
