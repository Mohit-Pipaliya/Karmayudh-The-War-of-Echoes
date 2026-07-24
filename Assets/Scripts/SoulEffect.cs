using UnityEngine;

public class SoulEffect : MonoBehaviour
{
    [Header("Soul Appearance")]
    public Color soulColor = new Color(0.1f, 0.8f, 1.0f, 0.8f); // Glowing Cyan/Light Blue
    public float pulseSpeed = 1.5f;
    public float pulseIntensity = 0.5f;

    private Material soulMaterial;

    void Start()
    {
        // 1. Create a highly glowing material (URP Compatible)
        Shader soulShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (soulShader == null) soulShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (soulShader == null) soulShader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (soulShader == null) soulShader = Shader.Find("Sprites/Default");

        soulMaterial = new Material(soulShader);
        
        // For URP Unlit, color is _BaseColor
        if (soulMaterial.HasProperty("_BaseColor"))
            soulMaterial.SetColor("_BaseColor", soulColor);
        else if (soulMaterial.HasProperty("_Color"))
            soulMaterial.SetColor("_Color", soulColor);
        else if (soulMaterial.HasProperty("_TintColor"))
            soulMaterial.SetColor("_TintColor", soulColor);

        // Try to enable strong emission
        soulMaterial.EnableKeyword("_EMISSION");
        if (soulMaterial.HasProperty("_EmissionColor"))
        {
            soulMaterial.SetColor("_EmissionColor", soulColor * 2.0f);
        }

        // Make it semi-transparent if possible in URP
        soulMaterial.SetFloat("_Surface", 1); // 1 = Transparent
        soulMaterial.SetFloat("_Blend", 0); // 0 = Alpha
        soulMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // 2. Apply this material to ALL meshes on the enemy
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r is ParticleSystemRenderer) continue;

            Material[] newMats = new Material[r.materials.Length];
            for (int i = 0; i < newMats.Length; i++)
            {
                newMats[i] = soulMaterial;
            }
            r.materials = newMats;
        }

        // 3. Add a realistic floating particle aura
        AddRealisticSoulParticles();
    }

    private void AddRealisticSoulParticles()
    {
        GameObject particlesObj = new GameObject("SoulAura_Realistic");
        particlesObj.transform.SetParent(transform);
        particlesObj.transform.localPosition = new Vector3(0, 1f, 0); 
        
        ParticleSystem ps = particlesObj.AddComponent<ParticleSystem>();
        
        // Main Module
        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(soulColor.r, soulColor.g, soulColor.b, 0.8f), new Color(1f, 1f, 1f, 0.5f));
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        main.gravityModifier = -0.02f; // Slow float upwards
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Leaves a trail when moving

        // Emission Module
        var emission = ps.emission;
        emission.rateOverTime = 40f;

        // Shape Module 
        var shape = ps.shape;
        SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null)
        {
            shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
            shape.skinnedMeshRenderer = smr;
            shape.normalOffset = 0.05f; // Push particles slightly outside the body
        }
        else
        {
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;
        }
        
        // Noise Module (Makes particles move like magical flames/souls)
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.2f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 1f;

        // Size Over Lifetime (Shrink as they die)
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0f);

        // Color Over Lifetime (Fade out smoothly)
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(soulColor, 0.5f), new GradientColorKey(Color.black, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.2f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        // Renderer
        ParticleSystemRenderer psr = particlesObj.GetComponent<ParticleSystemRenderer>();
        psr.material = soulMaterial;
        psr.renderMode = ParticleSystemRenderMode.Billboard;
    }

    void Update()
    {
        // 4. Pulse the brightness of the soul material over time
        if (soulMaterial != null)
        {
            float pulse = 1f + Mathf.PingPong(Time.time * pulseSpeed, pulseIntensity);
            Color currentColor = soulColor * pulse;
            
            if (soulMaterial.HasProperty("_BaseColor"))
                soulMaterial.SetColor("_BaseColor", currentColor);
            else if (soulMaterial.HasProperty("_TintColor"))
                soulMaterial.SetColor("_TintColor", currentColor);
                
            if (soulMaterial.HasProperty("_EmissionColor"))
                soulMaterial.SetColor("_EmissionColor", currentColor * 2.0f);
        }
    }
}
