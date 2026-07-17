using UnityEngine;

public class SoulEffect : MonoBehaviour
{
    [Header("Soul Appearance")]
    public Color soulColor = new Color(0.1f, 0.8f, 1.0f, 0.6f); // Glowing Cyan/Light Blue
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.3f;

    private Material soulMaterial;

    void Start()
    {
        // 1. Create a glowing, semi-transparent material
        Shader soulShader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (soulShader == null) soulShader = Shader.Find("Particles/Standard Unlit");
        if (soulShader == null) soulShader = Shader.Find("Sprites/Default");

        soulMaterial = new Material(soulShader);
        soulMaterial.SetColor("_TintColor", soulColor);
        soulMaterial.SetColor("_Color", soulColor);
        soulMaterial.SetColor("_EmissionColor", soulColor * 1.5f);
        soulMaterial.EnableKeyword("_EMISSION");

        // 2. Apply this material to ALL meshes on the enemy (body, clothes, weapons)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            // Skip the particle system renderer if it already exists
            if (r is ParticleSystemRenderer) continue;

            Material[] newMats = new Material[r.materials.Length];
            for (int i = 0; i < newMats.Length; i++)
            {
                newMats[i] = soulMaterial;
            }
            r.materials = newMats;
        }

        // 3. Add a cool floating particle aura
        AddSoulParticles();
    }

    private void AddSoulParticles()
    {
        GameObject particlesObj = new GameObject("SoulAura");
        particlesObj.transform.SetParent(transform);
        particlesObj.transform.localPosition = new Vector3(0, 1f, 0); // Center of body
        
        ParticleSystem ps = particlesObj.AddComponent<ParticleSystem>();
        
        // Main Module
        var main = ps.main;
        main.startColor = new Color(soulColor.r, soulColor.g, soulColor.b, 1f); // Opaque for particles
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1.0f);
        main.gravityModifier = -0.05f; // Float upwards gently
        main.maxParticles = 100;

        // Emission Module
        var emission = ps.emission;
        emission.rateOverTime = 30f;

        // Shape Module (Emit from the body mesh if possible)
        var shape = ps.shape;
        SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null)
        {
            shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
            shape.skinnedMeshRenderer = smr;
        }
        else
        {
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1f, 2f, 1f);
        }
        
        // Color Over Lifetime Module (Fade out)
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        // Renderer
        ParticleSystemRenderer psr = particlesObj.GetComponent<ParticleSystemRenderer>();
        psr.material = soulMaterial;
    }

    void Update()
    {
        // 4. Pulse the brightness of the soul material over time
        if (soulMaterial != null)
        {
            float pulse = 1f + Mathf.PingPong(Time.time * pulseSpeed, pulseIntensity);
            Color currentColor = soulColor * pulse;
            
            if (soulMaterial.HasProperty("_TintColor"))
                soulMaterial.SetColor("_TintColor", currentColor);
                
            if (soulMaterial.HasProperty("_EmissionColor"))
                soulMaterial.SetColor("_EmissionColor", currentColor * 1.5f);
        }
    }
}
