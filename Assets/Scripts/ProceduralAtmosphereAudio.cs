using UnityEngine;

/// <summary>
/// Generates a AAA Dark Ambient Drone sound procedurally!
/// No MP3 files needed. It uses pure mathematics to generate cinematic tension.
/// Perfect for Souls-like or Hell environments.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ProceduralAtmosphereAudio : MonoBehaviour
{
    [Header("Drone Settings")]
    [Range(0f, 1f)] public float globalVolume = 0.5f;
    public float baseFrequency = 55f; // Deep bass (A1)
    
    private float phase1;
    private float phase2;
    private float phase3;
    private float sampleRate;
    private float lfoPhase;

    void Start()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = true;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f; // 2D Background music
        
        // We don't actually need an AudioClip! 
        // OnAudioFilterRead will generate the audio directly into the audio buffer.
        audioSource.Play();

        sampleRate = AudioSettings.outputSampleRate;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        // 1. Calculate time increments based on sample rate
        float increment1 = baseFrequency * 2f * Mathf.PI / sampleRate;
        float increment2 = (baseFrequency * 1.5f) * 2f * Mathf.PI / sampleRate; // Perfect fifth
        float increment3 = (baseFrequency * 0.5f) * 2f * Mathf.PI / sampleRate; // Sub octave
        
        // Slow LFO for eerie volume swelling (1 cycle every 6 seconds)
        float lfoIncrement = (1f / 6f) * 2f * Mathf.PI / sampleRate;

        for (int i = 0; i < data.Length; i += channels)
        {
            // 2. Generate pure sine waves
            phase1 += increment1;
            phase2 += increment2;
            phase3 += increment3;
            lfoPhase += lfoIncrement;

            if (phase1 > 2f * Mathf.PI) phase1 -= 2f * Mathf.PI;
            if (phase2 > 2f * Mathf.PI) phase2 -= 2f * Mathf.PI;
            if (phase3 > 2f * Mathf.PI) phase3 -= 2f * Mathf.PI;
            if (lfoPhase > 2f * Mathf.PI) lfoPhase -= 2f * Mathf.PI;

            float wave1 = Mathf.Sin(phase1);
            float wave2 = Mathf.Sin(phase2) * 0.5f;
            float wave3 = Mathf.Sin(phase3) * 0.8f;

            // 3. Generate low-level dark noise (wind/rumble)
            float noise = Random.Range(-1f, 1f) * 0.05f;

            // 4. Modulate with slow LFO for "breathing" effect
            float lfo = (Mathf.Sin(lfoPhase) * 0.5f) + 0.5f; // Range 0 to 1
            float dynamicVolume = globalVolume * (0.3f + 0.7f * lfo);

            // 5. Combine everything
            float mixedSample = (wave1 + wave2 + wave3 + noise) * 0.3f * dynamicVolume;

            // 6. Output to all channels (Stereo/Mono)
            for (int c = 0; c < channels; c++)
            {
                data[i + c] = mixedSample;
            }
        }
    }
}
