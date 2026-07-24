using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class GuideEnemyManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The actual enemy or ghost model that will appear. Put it as a child of this object and drag it here.")]
    public GameObject guideModel;

    [Tooltip("Optional: Particle effect to play when the enemy appears or disappears.")]
    public GameObject teleportVFX;

    // Original Transform storage
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    // Status
    private bool isFinished = false;

    // Lists to remember what was originally enabled, so we can re-enable them later
    private List<Collider> disabledColliders = new List<Collider>();
    private List<Rigidbody> disabledRigidbodies = new List<Rigidbody>();
    private List<AudioSource> disabledAudios = new List<AudioSource>();
    private List<NavMeshAgent> disabledAgents = new List<NavMeshAgent>();
    private List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();

    private void Start()
    {
        if (guideModel != null)
        {
            // Yahan hum strictly Enemy Model ki asli global position save kar rahe hain
            originalPosition = guideModel.transform.position;
            originalRotation = guideModel.transform.rotation;

            MakeGhost(guideModel);
            guideModel.SetActive(false);
        }
    }

    private void MakeGhost(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            if (col.enabled)
            {
                disabledColliders.Add(col);
                col.enabled = false;
            }
        }

        Rigidbody[] rbs = obj.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rbs)
        {
            if (!rb.isKinematic)
            {
                disabledRigidbodies.Add(rb);
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
        }

        AudioSource[] audios = obj.GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audio in audios)
        {
            if (audio.enabled)
            {
                disabledAudios.Add(audio);
                audio.enabled = false;
                audio.Stop();
            }
        }

        NavMeshAgent[] agents = obj.GetComponentsInChildren<NavMeshAgent>();
        foreach (NavMeshAgent agent in agents)
        {
            if (agent.enabled)
            {
                disabledAgents.Add(agent);
                agent.enabled = false;
            }
        }

        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script == this) continue;
            if (script.GetType().Name == "SoulEffect") continue;
            
            if (script.enabled)
            {
                disabledScripts.Add(script);
                script.enabled = false;
            }
        }
    }

    private void RestoreRealEnemy()
    {
        foreach (Collider col in disabledColliders) if (col != null) col.enabled = true;
        foreach (Rigidbody rb in disabledRigidbodies) if (rb != null) { rb.isKinematic = false; rb.detectCollisions = true; }
        foreach (AudioSource audio in disabledAudios) if (audio != null) audio.enabled = true;
        
        foreach (NavMeshAgent agent in disabledAgents) 
        {
            if (agent != null) 
            {
                agent.enabled = true;
                // Unity requires warp sometimes if teleported while disabled
                agent.Warp(originalPosition); 
            }
        }

        foreach (MonoBehaviour script in disabledScripts) if (script != null) script.enabled = true;
        
        // Remove the Soul Effect if it exists so it looks like a normal enemy again
        SoulEffect soulEff = guideModel.GetComponentInChildren<SoulEffect>();
        if (soulEff != null)
        {
            soulEff.enabled = false; 
        }
    }

    public void ShowGuide(Vector3 position, Quaternion rotation, float duration)
    {
        if (isFinished) return; // Agar kahani khatam ho gayi toh aur kuch nahi hoga
        StopAllCoroutines();
        StartCoroutine(GuideSequence(position, rotation, duration, false));
    }

    public void ShowFinalGuide(Vector3 position, Quaternion rotation, float duration)
    {
        if (isFinished) return;
        StopAllCoroutines();
        StartCoroutine(GuideSequence(position, rotation, duration, true));
    }

    private IEnumerator GuideSequence(Vector3 position, Quaternion rotation, float duration, bool isFinal)
    {
        // Checkpoint position par jao
        transform.position = position;
        transform.rotation = rotation;

        if (guideModel != null)
        {
            guideModel.transform.localPosition = Vector3.zero;
            guideModel.transform.localRotation = Quaternion.identity;
        }

        if (teleportVFX != null) Instantiate(teleportVFX, position, rotation);
        if (guideModel != null) guideModel.SetActive(true);

        // Wait karo 1 ya 2 second
        yield return new WaitForSeconds(duration);

        if (teleportVFX != null) Instantiate(teleportVFX, position, rotation);
        
        if (!isFinal)
        {
            // Normal checkpoint: Hide enemy
            if (guideModel != null) guideModel.SetActive(false);
        }
        else
        {
            // Last checkpoint: Teleport to original place and turn into a real enemy
            isFinished = true; // Iske baad dusre purane checkpoints kaam nahi karenge

            // Manager aur model dono ko original place par wapas bhejo
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            
            if (guideModel != null)
            {
                guideModel.transform.position = originalPosition;
                guideModel.transform.rotation = originalRotation;
            }

            if (teleportVFX != null) Instantiate(teleportVFX, originalPosition, originalRotation);

            RestoreRealEnemy();
            
            // Unparent kar do taaki manager se azaad ho jaye
            if (guideModel != null) guideModel.transform.SetParent(null);

            Debug.Log("Guide Enemy wapas apni jagah par pahunch gaya aur real ban gaya!");
        }
    }
}
