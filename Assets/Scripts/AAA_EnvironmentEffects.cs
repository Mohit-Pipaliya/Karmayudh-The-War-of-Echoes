using UnityEngine;

public class AAA_EnvironmentEffects : MonoBehaviour
{
    public Transform player; // Assign in inspector, or we find it
    
    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else if (FindObjectOfType<PlayerController>() != null) player = FindObjectOfType<PlayerController>().transform;
        }

        EnhanceLighting();
        CreateAtmosphericParticles();
    }

    void EnhanceLighting()
    {
        // Find the main directional light (Sun)
        Light[] lights = FindObjectsOfType<Light>();
        Light sun = null;
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                sun = l;
                break;
            }
        }

        if (sun != null)
        {
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f; // Softer shadows
            sun.color = new Color(1.0f, 0.95f, 0.85f); // Slightly warm sunset/sunrise color
            sun.intensity = Mathf.Max(sun.intensity, 1.2f); // Ensure it's bright enough
        }

        // Enhance ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.6f, 0.7f, 0.9f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.45f, 0.5f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f);
        
        // Add subtle exponential fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.005f;
        RenderSettings.fogColor = new Color(0.6f, 0.7f, 0.8f);

        Debug.Log("AAA Lighting & Fog Setup Complete!");
    }

    void CreateAtmosphericParticles()
    {
        if (player == null) return;

        // Create a particle system for floating dust/spores/leaves
        GameObject particleObj = new GameObject("AAA_AtmosphericDust");
        particleObj.transform.SetParent(player); // Follow player
        particleObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startColor = new Color(1f, 1f, 1f, 0.5f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300; // Low count = no FPS drop

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 15f; // Surround the player

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f); // Drift up and down gently

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.2f;
        noise.frequency = 0.5f;

        // Set rendering material to default particle
        ParticleSystemRenderer renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetFloat("_Mode", 2); // Fade mode
        renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        renderer.material.SetInt("_ZWrite", 0);
        renderer.material.DisableKeyword("_ALPHATEST_ON");
        renderer.material.EnableKeyword("_ALPHABLEND_ON");
        renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        renderer.material.renderQueue = 3000;
        
        // Add particle system to play
        ps.Play();
        
        Debug.Log("AAA Atmospheric Dust Setup Complete!");
    }
}
