using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject loadingPanel;
    public GameObject mainMenuPanel;
    public GameObject optionMenuPanel;
    public GameObject pauseMenuPanel;
    public GameObject gameOverPanel;
    public GameObject finishPanel;
    public GameObject healthBarPanel;

    [Header("UI Elements")]
    public Slider loadingSlider;
    public Slider volumeSlider;

    [Header("Settings")]
    public float loadingTime = 3f; // Fake loading time

    private bool isPaused = false;
    private bool isGameOver = false;
    private bool isFinished = false;

    private enum OptionMenuSource { MainMenu, PauseMenu }
    private OptionMenuSource currentOptionSource;

    public static bool isRespawning = false;

    void Awake()
    {
        if (isRespawning)
        {
            // If respawning, instantly hide loading screen if it was active in the scene
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        }
    }

    void Start()
    {
        if (isRespawning)
        {
            // If we are respawning from a checkpoint, skip the main menu and go straight to game
            isRespawning = false;
            PlayGame();
        }
        else
        {
            // Initial setup: Show Loading Screen
            ShowPanel(loadingPanel);
            if (healthBarPanel != null) healthBarPanel.SetActive(false);
            
            StartCoroutine(SimulateLoading());
        }

        if(volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
            // Default volume value set to current AudioListener volume
            volumeSlider.value = AudioListener.volume; 
        }
    }

    void Update()
    {
        // Null check for keyboard to avoid errors if no keyboard is connected
        if (Keyboard.current == null) return;

        // Pause logic
        bool isEscapePressed = Keyboard.current.escapeKey.wasPressedThisFrame;
        bool isLoadingActive = loadingPanel != null && loadingPanel.activeSelf;
        bool isMainMenuActive = mainMenuPanel != null && mainMenuPanel.activeSelf;

        if (isEscapePressed && !isGameOver && !isFinished && !isLoadingActive && !isMainMenuActive)
        {
            if (isPaused)
            {
                // If we are in option menu from pause menu, back goes to pause menu
                if (optionMenuPanel != null && optionMenuPanel.activeSelf)
                {
                    BackButton();
                }
                else
                {
                    ResumeGame();
                }
            }
            else
            {
                PauseGame();
            }
        }

        // Restart logic
        if (isGameOver && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartGame();
        }
    }

    IEnumerator SimulateLoading()
    {
        float timer = 0f;
        while (timer < loadingTime)
        {
            timer += Time.deltaTime;
            if (loadingSlider != null)
            {
                loadingSlider.value = timer / loadingTime;
            }
            yield return null;
        }

        // Loading finished, show main menu
        ShowPanel(mainMenuPanel);
    }

    public void ShowPanel(GameObject panelToShow)
    {
        if (loadingPanel != null && panelToShow != loadingPanel) loadingPanel.SetActive(false);
        if (mainMenuPanel != null && panelToShow != mainMenuPanel) mainMenuPanel.SetActive(false);
        if (optionMenuPanel != null && panelToShow != optionMenuPanel) optionMenuPanel.SetActive(false);
        if (pauseMenuPanel != null && panelToShow != pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (gameOverPanel != null && panelToShow != gameOverPanel) gameOverPanel.SetActive(false);
        if (finishPanel != null && panelToShow != finishPanel) finishPanel.SetActive(false);
        // We don't hide healthBarPanel here because it stays on during gameplay, handled separately

        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
            
            // AAA UI Animation (Scale and Fade)
            StartCoroutine(AnimatePanelIn(panelToShow));

            // Unlock cursor for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Lock cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    IEnumerator AnimatePanelIn(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        panel.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Smooth ease out
            float easeOut = 1f - Mathf.Pow(1f - t, 3f);
            
            cg.alpha = easeOut;
            float scale = Mathf.Lerp(0.9f, 1f, easeOut);
            panel.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        cg.alpha = 1f;
        panel.transform.localScale = Vector3.one;
    }

    // ================= MAIN MENU =================
    public void PlayGame()
    {
        ShowPanel(null); // Hide all main panels
        if (healthBarPanel != null) healthBarPanel.SetActive(true);
        Time.timeScale = 1f;
        isPaused = false;
        isGameOver = false;
        isFinished = false;
    }

    public void OpenOptionFromMainMenu()
    {
        currentOptionSource = OptionMenuSource.MainMenu;
        ShowPanel(optionMenuPanel);
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit!");
        Application.Quit();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ================= PAUSE MENU =================
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        ShowPanel(pauseMenuPanel);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        ShowPanel(null); // Hide pause menu
    }

    public void OpenOptionFromPauseMenu()
    {
        currentOptionSource = OptionMenuSource.PauseMenu;
        ShowPanel(optionMenuPanel);
    }

    public void ReturnToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (healthBarPanel != null) healthBarPanel.SetActive(false);
        ShowPanel(mainMenuPanel);
    }

    // ================= OPTION MENU =================
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void BackButton()
    {
        if (currentOptionSource == OptionMenuSource.MainMenu)
        {
            ShowPanel(mainMenuPanel);
        }
        else if (currentOptionSource == OptionMenuSource.PauseMenu)
        {
            ShowPanel(pauseMenuPanel);
        }
    }

    // ================= GAMEPLAY TRIGGERS =================
    public void PlayerDied()
    {
        isGameOver = true;
        Time.timeScale = 0f; // Pause game logic on game over
        ShowPanel(gameOverPanel);
        if (healthBarPanel != null) healthBarPanel.SetActive(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // Unfreeze game
        
        // Mark that we are respawning so the Main Menu doesn't show up again
        isRespawning = true;
        
        // Reload the scene. This perfectly resets ALL enemies, triggers, and the world state.
        // The PlayerController's Start() will automatically place the player at the lastCheckpointPosition.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GameFinished()
    {
        isFinished = true;
        Time.timeScale = 0f;
        ShowPanel(finishPanel);
        if (healthBarPanel != null) healthBarPanel.SetActive(false);
    }
}
