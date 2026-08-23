using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; 
    public float distance = 6.0f; 
    public float heightOffset = 1.5f; 

    [Header("Orbit Settings")]
    public float sensitivityX = 30.0f; 
    
    [Header("Camera Fixed Height Angle")]
    public float fixedAngleY = 15f; 

    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;
    private float currentX = 0f;

    [Header("AAA Camera Effects")]
    public bool enableBreathing = true;
    public float breathingSpeed = 1.5f;
    public float breathingMagnitude = 0.05f;
    public bool enableDynamicFOV = true;
    public float baseFOV = 60f;
    public float runFOV = 70f;
    public float fovTransitionSpeed = 5f;

    private Camera cam;
    private PlayerController playerController;

    // Cinematic variables
    private bool isCinematic = false;
    private Transform cinematicEnemy;
    private float cinematicTransitionSpeed = 2f;
    private Vector3 cinematicTargetPosition;
    private Quaternion cinematicTargetRotation;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null) cam.fieldOfView = baseFOV;
        
        if (target != null)
        {
            playerController = target.GetComponent<PlayerController>();
        }
        // Cursor lock logic is now handled by UIManager
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Agar cutscene chal raha hai toh cinematic logic chalega
        if (isCinematic && cinematicEnemy != null)
        {
            HandleCinematicCamera();
        }
        else
        {
            HandleNormalCamera();
        }
    }

    void HandleNormalCamera()
    {
        // Jab cursor lock ho, tabhi mouse se camera ghumana hai (taki Main Menu me cursor hilanese camera na ghume)
        if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
        {
            currentX += Mouse.current.delta.x.ReadValue() * sensitivityX * Time.unscaledDeltaTime;
        }

        Quaternion rotation = Quaternion.Euler(fixedAngleY, currentX, 0);
        Vector3 targetPos = target.position + Vector3.up * heightOffset;
        Vector3 desiredPosition = targetPos - (rotation * Vector3.forward * distance);

        // Camera Collision - SphereCastAll taaki camera deewar ke andar clip na ho
        float cameraRadius = 0.3f; // Camera ki motai (radius)
        Vector3 direction = (desiredPosition - targetPos).normalized;
        float closestDistance = distance;

        RaycastHit[] hits = Physics.SphereCastAll(targetPos, cameraRadius, direction, distance);
        
        foreach (RaycastHit h in hits)
        {
            // Player aur Triggers se camera na takraye
            if (!h.collider.CompareTag("Player") && !h.collider.isTrigger)
            {
                if (h.distance < closestDistance)
                {
                    closestDistance = h.distance;
                }
            }
        }
        
        // Deewar milne par camera ko aage kar do
        desiredPosition = targetPos + direction * closestDistance;

        if (shakeDuration > 0)
        {
            desiredPosition += Random.insideUnitSphere * shakeMagnitude;
            shakeDuration -= Time.unscaledDeltaTime; 
        }

        // AAA Breathing Effect
        if (enableBreathing && shakeDuration <= 0)
        {
            float noiseX = (Mathf.PerlinNoise(Time.time * breathingSpeed, 0f) - 0.5f) * breathingMagnitude;
            float noiseY = (Mathf.PerlinNoise(0f, Time.time * breathingSpeed) - 0.5f) * breathingMagnitude;
            desiredPosition += rotation * new Vector3(noiseX, noiseY, 0f);
        }

        // AAA Dynamic FOV
        if (enableDynamicFOV && cam != null)
        {
            float targetFOV = baseFOV;
            if (playerController != null && Keyboard.current != null)
            {
                bool isRunning = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
                bool isMoving = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed ||
                                Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ||
                                Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ||
                                Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
                
                if (isRunning && isMoving) targetFOV = runFOV;
            }
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovTransitionSpeed * Time.unscaledDeltaTime);
        }

        transform.position = desiredPosition;
        transform.rotation = rotation;
    }

    void HandleCinematicCamera()
    {
        // Dono characters ke beech ka center point
        Vector3 midPoint = (target.position + cinematicEnemy.position) / 2f;
        midPoint.y += heightOffset; // Thoda upar dekhe

        // Dono characters ke beech ki line nikali
        Vector3 lineBetween = (cinematicEnemy.position - target.position).normalized;

        // Us line se 90 degree ghoom gaye (side view ke liye)
        Vector3 sideDirection = Vector3.Cross(Vector3.up, lineBetween).normalized;
        
        // Agar angle theek na aye, toh aap direction invert kar sakte hain sideDirection *= -1;
        
        // Side view position calculate ki
        cinematicTargetPosition = midPoint + sideDirection * (distance * 0.8f);

        // Center point ki taraf dekhna hai
        cinematicTargetRotation = Quaternion.LookRotation(midPoint - cinematicTargetPosition);

        // Smoothly wahan jana
        transform.position = Vector3.Slerp(transform.position, cinematicTargetPosition, cinematicTransitionSpeed * Time.unscaledDeltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, cinematicTargetRotation, cinematicTransitionSpeed * Time.unscaledDeltaTime);
    }

    public void StartCinematic(Transform enemyTransform)
    {
        isCinematic = true;
        cinematicEnemy = enemyTransform;
    }

    public void StopCinematic()
    {
        isCinematic = false;
        cinematicEnemy = null;
        
        // Jab cinematic khatam ho, toh camera dobara player ke peeche aane lage uske liye currentX set kar diya
        if (target != null)
        {
            currentX = target.eulerAngles.y; 
        }
    }

    public void TriggerShake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }
}
