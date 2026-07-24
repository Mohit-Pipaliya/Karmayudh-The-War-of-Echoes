using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// AAA Game Feel Manager (God of War style impact).
/// Call CinematicCombatEffects.Instance.TriggerHeavyHit() from your weapon collision script!
/// </summary>
public class CinematicCombatEffects : MonoBehaviour
{
    public static CinematicCombatEffects Instance;

    [Header("Camera Shake Settings")]
    public float lightShakeDuration = 0.1f;
    public float lightShakeMagnitude = 0.05f;
    public float heavyShakeDuration = 0.2f;
    public float heavyShakeMagnitude = 0.2f;

    [Header("Hit Stop Settings (Time Freeze)")]
    public float hitStopDuration = 0.05f;
    public float hitStopTimeScale = 0.1f;

    [Header("Debug")]
    public bool testEffectsOnLeftClick = true; // For immediate gamejam feedback!

    private Vector3 originalCameraLocalPos;
    private Coroutine shakeCoroutine;
    private Coroutine hitStopCoroutine;
    private Camera mainCam;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        // Immediate gamejam juice: light shake on every mouse click (sword swing)
        if (testEffectsOnLeftClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TriggerLightSwing();
        }
    }

    /// <summary>
    /// Call this when the player SWINGS the sword (misses).
    /// </summary>
    public void TriggerLightSwing()
    {
        if (mainCam != null)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeRoutine(lightShakeDuration, lightShakeMagnitude));
        }
    }

    /// <summary>
    /// Call this when the sword actually HITS an enemy!
    /// </summary>
    public void TriggerHeavyHit()
    {
        // 1. Heavy Camera Shake
        if (mainCam != null)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeRoutine(heavyShakeDuration, heavyShakeMagnitude));
        }

        // 2. Hit Stop (Freeze time for impact)
        if (hitStopCoroutine != null) StopCoroutine(hitStopCoroutine);
        hitStopCoroutine = StartCoroutine(HitStopRoutine());
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        originalCameraLocalPos = mainCam.transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            mainCam.transform.localPosition = new Vector3(originalCameraLocalPos.x + x, originalCameraLocalPos.y + y, originalCameraLocalPos.z);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        mainCam.transform.localPosition = originalCameraLocalPos;
    }

    private IEnumerator HitStopRoutine()
    {
        Time.timeScale = hitStopTimeScale;
        // Wait using unscaled time so the wait isn't affected by the slow-mo!
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
    }
}
