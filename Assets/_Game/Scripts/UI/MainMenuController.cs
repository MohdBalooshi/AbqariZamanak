using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnAchievements;
    [SerializeField] private Button btnSettings;

    [Header("Panels (optional, if you use in-scene panels)")]
    [SerializeField] private GameObject achievementsPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Scene Names (if using separate scenes)")]
    [SerializeField] private string categorySceneName = "CategoryScene";
    [SerializeField] private string achievementsSceneName = "";  // leave empty if using panel
    [SerializeField] private string settingsSceneName = "";      // leave empty if using panel

    private void Awake()
    {
        // Load saves if launched directly into this scene
        if (SaveSystem.Data == null)
            SaveSystem.Load();
    }

    private void Start()
    {
        // Safe subscriptions
        if (btnStart)
        {
            btnStart.onClick.RemoveAllListeners();
            btnStart.onClick.AddListener(OnStartClicked);
        }
        if (btnAchievements)
        {
            btnAchievements.onClick.RemoveAllListeners();
            btnAchievements.onClick.AddListener(OnAchievementsClicked);
        }
        if (btnSettings)
        {
            btnSettings.onClick.RemoveAllListeners();
            btnSettings.onClick.AddListener(OnSettingsClicked);
        }

        // Ensure optional panels are hidden by default
        if (achievementsPanel) achievementsPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    private void OnStartClicked()
    {
        // Go to category selection → then LevelSelect → Quiz
        if (!string.IsNullOrEmpty(categorySceneName))
        {
            if (!Application.CanStreamedLevelBeLoaded(categorySceneName))
            {
                Debug.LogError($"[MainMenuController] Scene '{categorySceneName}' not in Build Settings.");
                return;
            }
            SceneManager.LoadScene(categorySceneName);
        }
    }

    private void OnAchievementsClicked()
    {
        // If you use a panel in this same scene:
        if (achievementsPanel)
        {
            achievementsPanel.SetActive(true);
            return;
        }
        // Or a separate scene:
        if (!string.IsNullOrEmpty(achievementsSceneName))
        {
            if (!Application.CanStreamedLevelBeLoaded(achievementsSceneName))
            {
                Debug.LogError($"[MainMenuController] Scene '{achievementsSceneName}' not in Build Settings.");
                return;
            }
            SceneManager.LoadScene(achievementsSceneName);
        }
    }

    private void OnSettingsClicked()
    {
        // If you use a panel in this same scene:
        if (settingsPanel)
        {
            settingsPanel.SetActive(true);
            return;
        }
        // Or a separate scene:
        if (!string.IsNullOrEmpty(settingsSceneName))
        {
            if (!Application.CanStreamedLevelBeLoaded(settingsSceneName))
            {
                Debug.LogError($"[MainMenuController] Scene '{settingsSceneName}' not in Build Settings.");
                return;
            }
            SceneManager.LoadScene(settingsSceneName);
        }
    }

    // Optional close hooks for panel X buttons
    public void CloseAchievements() { if (achievementsPanel) achievementsPanel.SetActive(false); }
    public void CloseSettings()     { if (settingsPanel)     settingsPanel.SetActive(false); }
}
