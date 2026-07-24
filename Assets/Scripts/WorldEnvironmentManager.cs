using UnityEngine;

public class WorldEnvironmentManager : MonoBehaviour
{
    public static WorldEnvironmentManager Instance;

    private Transform player;
    private Camera mainCam;

    [Header("Worlds Setup")]
    public string soulWorldName = "Soul World";
    public string physicalWorldName = "Physical World";
    public string angryWorldName = "Angry World";

    private Transform soulWorld;
    private Transform physicalWorld;
    private Transform angryWorld;

    [Header("BGM Setup (Drag your AudioClips here)")]
    public AudioClip soulWorldBGM;
    public AudioClip physicalWorldBGM;
    public AudioClip angryWorldBGM;
    [Range(0f, 1f)] public float bgmMaxVolume = 0.5f;

    private AudioSource bgmSource;
    private AudioClip targetBGMClip;
    private int bgmSuppressionCount = 0; // If > 0, music fades out (for cutscenes/fights)

    [Header("Soul World Settings")]
    public Color soulFogColor = new Color(0.1f, 0.4f, 0.8f);
    public float soulFogDensity = 0.015f; // Realistic mist

    [Header("Physical World Settings (Heaven)")]
    public Color physicalFogColor = new Color(1f, 0.9f, 0.7f);
    public float physicalFogDensity = 0.005f; // Light haze

    [Header("Angry World Settings (Hell)")]
    public Color angryFogColor = new Color(0.7f, 0.1f, 0.05f);
    public float angryFogDensity = 0.025f; // Thick smoke

    [Header("Transition Settings")]
    public float transitionSpeed = 0.5f;

    private Color targetFogColor;
    private float targetFogDensity;

    private float searchTimer = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        
        mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor; // REQUIRED for seamless AAA fog
        }
        
        // Setup BGM Source
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f; // 2D Background Music
        bgmSource.volume = 0f;

        // Start with default neutral fog
        targetFogColor = RenderSettings.fogColor;
        targetFogDensity = 0.01f; // Thicker base fog
    }

    void Update()
    {
        // Find references dynamically every 2 seconds if they are missing
        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0)
        {
            FindReferences();
            searchTimer = 2f;
        }

        if (player == null) return;

        DetermineClosestWorld();
        ApplySmoothTransition();
    }

    void FindReferences()
    {
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        if (soulWorld == null)
        {
            GameObject sw = GameObject.Find(soulWorldName);
            if (sw != null) soulWorld = sw.transform;
        }

        if (physicalWorld == null)
        {
            GameObject pw = GameObject.Find(physicalWorldName);
            if (pw != null) physicalWorld = pw.transform;
        }

        if (angryWorld == null)
        {
            GameObject aw = GameObject.Find(angryWorldName);
            if (aw != null) angryWorld = aw.transform;
        }
    }

    void DetermineClosestWorld()
    {
        float minDist = float.MaxValue;
        Transform closestWorld = null;

        if (soulWorld != null)
        {
            float dist = Vector3.Distance(player.position, soulWorld.position);
            if (dist < minDist)
            {
                minDist = dist;
                closestWorld = soulWorld;
            }
        }

        if (physicalWorld != null)
        {
            float dist = Vector3.Distance(player.position, physicalWorld.position);
            if (dist < minDist)
            {
                minDist = dist;
                closestWorld = physicalWorld;
            }
        }

        if (angryWorld != null)
        {
            float dist = Vector3.Distance(player.position, angryWorld.position);
            if (dist < minDist)
            {
                minDist = dist;
                closestWorld = angryWorld;
            }
        }

        // Set target values based on closest world
        if (closestWorld == soulWorld)
        {
            targetFogColor = soulFogColor;
            targetFogDensity = soulFogDensity;
            targetBGMClip = soulWorldBGM;
        }
        else if (closestWorld == physicalWorld)
        {
            targetFogColor = physicalFogColor;
            targetFogDensity = physicalFogDensity;
            targetBGMClip = physicalWorldBGM;
        }
        else if (closestWorld == angryWorld)
        {
            targetFogColor = angryFogColor;
            targetFogDensity = angryFogDensity;
            targetBGMClip = angryWorldBGM;
        }
    }

    void ApplySmoothTransition()
    {
        // Lerp Fog settings
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetFogColor, Time.deltaTime * transitionSpeed);
        RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetFogDensity, Time.deltaTime * transitionSpeed);

        // Also lerp Camera background color to match fog (seamless horizon)
        if (mainCam != null)
        {
            mainCam.backgroundColor = Color.Lerp(mainCam.backgroundColor, targetFogColor, Time.deltaTime * transitionSpeed);
        }

        // Crossfade BGM
        if (bgmSource != null)
        {
            if (bgmSource.clip != targetBGMClip)
            {
                // Fade out current clip before switching
                bgmSource.volume -= Time.deltaTime * transitionSpeed;
                if (bgmSource.volume <= 0.01f)
                {
                    bgmSource.clip = targetBGMClip;
                    if (targetBGMClip != null) bgmSource.Play();
                }
            }
            else
            {
                // If suppressed (cutscene/fight), fade to 0. Otherwise fade to max volume.
                float targetVol = (bgmSuppressionCount > 0 || targetBGMClip == null) ? 0f : bgmMaxVolume;
                bgmSource.volume = Mathf.MoveTowards(bgmSource.volume, targetVol, Time.deltaTime * transitionSpeed);
            }
        }
    }

    // Call these from EnemyAI when cutscene/combat starts or ends!
    public void AddBGMSuppression() { bgmSuppressionCount++; }
    public void RemoveBGMSuppression() { bgmSuppressionCount = Mathf.Max(0, bgmSuppressionCount - 1); }
}
