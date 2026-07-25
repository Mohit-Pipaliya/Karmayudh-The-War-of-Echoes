using UnityEngine;

public class SoulEffect : MonoBehaviour
{
    [Header("Soul Appearance")]
    public Color soulColor = new Color(0.1f, 0.8f, 1.0f, 0.8f); // Glowing Cyan/Light Blue
    public float pulseSpeed = 1.5f;
    public float pulseIntensity = 0.5f;

    private Material[] soulMaterials;

    void Start()
    {
        // 1. Get all renderers on the enemy
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        System.Collections.Generic.List<Material> matList = new System.Collections.Generic.List<Material>();

        foreach (Renderer r in renderers)
        {
            if (r is ParticleSystemRenderer) continue;

            // 2. Clone the existing materials so we don't affect the original prefab/assets
            Material[] newMats = new Material[r.materials.Length];
            for (int i = 0; i < newMats.Length; i++)
            {
                newMats[i] = new Material(r.materials[i]);
                
                // Modify the material to look like a glowing soul
                if (newMats[i].HasProperty("_BaseColor"))
                    newMats[i].SetColor("_BaseColor", soulColor);
                else if (newMats[i].HasProperty("_Color"))
                    newMats[i].SetColor("_Color", soulColor);
                
                // Enable emission for the glow effect
                newMats[i].EnableKeyword("_EMISSION");
                if (newMats[i].HasProperty("_EmissionColor"))
                {
                    newMats[i].SetColor("_EmissionColor", soulColor * 2.0f);
                }

                matList.Add(newMats[i]);
            }
            r.materials = newMats;
        }

        soulMaterials = matList.ToArray();

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
        
        // Try to use a default particle material if available, otherwise just use standard
        Material particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        if(particleMat.shader.name == "Hidden/InternalErrorShader") 
            particleMat = new Material(Shader.Find("Particles/Standard Unlit"));
            
        particleMat.SetColor("_BaseColor", soulColor);
        particleMat.SetColor("_TintColor", soulColor);
        psr.material = particleMat;
        psr.renderMode = ParticleSystemRenderMode.Billboard;
    }

    void Update()
    {
        // 4. Pulse the brightness of the soul material over time
        if (soulMaterials != null && soulMaterials.Length > 0)
        {
            float pulse = 1f + Mathf.PingPong(Time.time * pulseSpeed, pulseIntensity);
            Color currentColor = soulColor * pulse;
            
            foreach (Material mat in soulMaterials)
            {
                if (mat != null)
                {
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", currentColor);
                    else if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", currentColor);
                        
                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", currentColor * 2.0f);
                }
            }
        }
    }
}
