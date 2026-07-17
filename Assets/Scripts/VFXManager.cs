using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static void SpawnHitVFX(Vector3 position, bool isBlood = false)
    {
        // Create a temporary GameObject
        GameObject vfxObject = new GameObject("HitVFX");
        vfxObject.transform.position = position;

        // Add a ParticleSystem
        ParticleSystem ps = vfxObject.AddComponent<ParticleSystem>();
        
        // Configure Particle System Main Module
        var main = ps.main;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.loop = false;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        if (isBlood)
        {
            main.startColor = new Color(0.8f, 0.1f, 0.1f, 1f); // Blood Red
        }
        else
        {
            main.startColor = new Color(1f, 0.8f, 0.2f, 1f); // Spark Yellow/Orange
        }

        // Configure Emission Module
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        ParticleSystem.Burst burst = new ParticleSystem.Burst(0f, (short)20, (short)40);
        emission.SetBursts(new ParticleSystem.Burst[] { burst });

        // Configure Shape Module
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        
        // Configure Collision Module (optional, to make them bounce off ground)
        var collision = ps.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.bounce = 0.2f;
        collision.dampen = 0.5f;

        // Auto destroy after the effect is done
        Destroy(vfxObject, 1.5f);
    }
}
