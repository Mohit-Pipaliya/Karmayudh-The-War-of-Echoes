using UnityEngine;
using System.Collections.Generic;

public class EnemyHealthBar : MonoBehaviour
{
    private GameObject ringCenter;
    private List<SpriteRenderer> healthSegments = new List<SpriteRenderer>();

    [Header("Raji Ground Circle Settings")]
    public int segmentCount = 12; // Perfect circle of runes
    public float radius = 0.6f; // Tighter radius around the feet
    public Vector2 segmentSize = new Vector2(0.25f, 0.06f); // Thicker, pill-shaped dashes

    public Color activeColor = new Color(0.2f, 0.9f, 1f, 1f); 
    public Color inactiveColor = new Color(0f, 0f, 0f, 0f); // Invisible when health is lost
    
    private float targetFill = 1f;
    private bool isInitialized = false;

    void Start()
    {
        if (!isInitialized)
        {
            Initialize(activeColor);
        }
    }

    public void Initialize(Color glowColor)
    {
        if (isInitialized) return;
        
        // Multiply RGB by a sensible HDR value
        activeColor = new Color(glowColor.r * 2f, glowColor.g * 2f, glowColor.b * 2f, 1f);

        // Create a CRISP, Anti-Aliased "Pill" (Rounded Rectangle) texture
        int texWidth = 128;
        int texHeight = 32;
        Texture2D pillTex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
        float r = texHeight / 2f;
        
        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                float dist = 0f;
                if (x < r) 
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
                else if (x > texWidth - r) 
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(texWidth - r, r));
                else 
                    dist = Mathf.Abs(y - r); 

                float alpha = Mathf.Clamp01(r - dist);
                pillTex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        pillTex.Apply();
        
        // Create Sprite with center pivot
        Sprite finalSprite = Sprite.Create(pillTex, new Rect(0, 0, texWidth, texHeight), new Vector2(0.5f, 0.5f), 100f);

        // 1. Create Ring Center at the Enemy's feet
        ringCenter = new GameObject("GroundHealthRing");
        // IMPORTANT: Parent ko null rakh rahe hain taaki enemy ka bada scale (jaise 100x) ispe asar na kare
        ringCenter.transform.SetParent(null); 
        ringCenter.transform.position = transform.position + new Vector3(0, 0.2f, 0); 
        
        // Try to find a URP compatible unlit sprite shader
        Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (spriteShader == null) spriteShader = Shader.Find("Sprites/Default");
        
        Material spriteMat = null;
        if (spriteShader != null)
        {
            spriteMat = new Material(spriteShader);
        }

        // Calculate exact scale needed to match segmentSize in world units
        float scaleX = segmentSize.x / (texWidth / 100f);
        float scaleY = segmentSize.y / (texHeight / 100f);

        // 2. Create the segmented circular dashes
        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segGO = new GameObject("Segment_" + i);
            segGO.transform.SetParent(ringCenter.transform, false);
            
            float angleDeg = i * (360f / segmentCount);
            float angleRad = angleDeg * Mathf.Deg2Rad;
            
            // Position in XZ plane relative to center
            Vector3 pos = new Vector3(Mathf.Cos(angleRad) * radius, 0, Mathf.Sin(angleRad) * radius);
            
            SpriteRenderer sr = segGO.AddComponent<SpriteRenderer>();
            sr.sprite = finalSprite; 
            sr.color = activeColor;
            
            if (spriteMat != null)
            {
                sr.material = spriteMat;
            }
            
            segGO.transform.localPosition = pos;
            segGO.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            
            // Lay flat on ground (rotate 90 on X), and point tangentially to form a ring (rotate angleDeg + 90 on Z)
            segGO.transform.localRotation = Quaternion.Euler(90f, 0, angleDeg + 90f);

            healthSegments.Add(sr);
        }
        
        isInitialized = true;
    }

    void LateUpdate()
    {
        if (ringCenter != null)
        {
            // 1. Raycast to find exact terrain height dynamically
            Vector3 targetPos = transform.position;
            RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 2f, Vector3.down, 5f);
            float highestGroundY = float.MinValue;
            Vector3 groundNormal = Vector3.up;
            bool foundGround = false;

            foreach (var hit in hits)
            {
                // Ignore enemy's own colliders and triggers
                if (hit.collider.transform.root == this.transform.root) continue;
                if (hit.collider.isTrigger) continue;

                if (hit.point.y > highestGroundY)
                {
                    highestGroundY = hit.point.y;
                    groundNormal = hit.normal;
                    foundGround = true;
                }
            }

            if (foundGround)
            {
                targetPos.y = highestGroundY;
            }

            // Place slightly above terrain to prevent clipping
            ringCenter.transform.position = targetPos + new Vector3(0, 0.15f, 0);
            
            // Align the ring perfectly with the terrain's slope (normal) so it doesn't slice through the character
            ringCenter.transform.rotation = Quaternion.FromToRotation(Vector3.up, groundNormal);
        }
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthSegments == null || healthSegments.Count == 0) return;

        targetFill = Mathf.Clamp01((float)currentHealth / maxHealth);
        int activeCount = Mathf.CeilToInt(targetFill * segmentCount);

        for (int i = 0; i < segmentCount; i++)
        {
            if (i >= healthSegments.Count || healthSegments[i] == null) continue;

            if (i < activeCount)
            {
                healthSegments[i].gameObject.SetActive(true);
            }
            else
            {
                healthSegments[i].gameObject.SetActive(false);
            }
        }
    }

    public void HideBar()
    {
        if (ringCenter != null)
        {
            ringCenter.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        // Jab enemy delete ho, toh uska health bar object bhi delete hona chahiye
        if (ringCenter != null)
        {
            Destroy(ringCenter);
        }
    }
}
