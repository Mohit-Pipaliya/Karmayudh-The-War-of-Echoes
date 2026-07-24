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

    // Cinematic variables
    private bool isCinematic = false;
    private Transform cinematicEnemy;
    private float cinematicTransitionSpeed = 2f;
    private Vector3 cinematicTargetPosition;
    private Quaternion cinematicTargetRotation;

    void Start()
    {
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

        // Camera Collision - taki camera deewar ke andar na ghuse
        RaycastHit hit;
        if (Physics.Linecast(targetPos, desiredPosition, out hit))
        {
            // Player ya kisi trigger (jaise check point) se collide nahi karna chahiye
            if (!hit.collider.CompareTag("Player") && !hit.collider.isTrigger)
            {
                desiredPosition = hit.point + (rotation * Vector3.forward * 0.1f);
            }
        }

        if (shakeDuration > 0)
        {
            desiredPosition += Random.insideUnitSphere * shakeMagnitude;
            shakeDuration -= Time.unscaledDeltaTime; 
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
