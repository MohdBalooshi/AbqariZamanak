using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnAchievements;
    [SerializeField] private Button btnSettings;

    [Header("Panels (optional)")]
    [SerializeField] private GameObject achievementsPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Scene Names")]
    [SerializeField] private string categorySceneName = "CategoryScene";
    [SerializeField] private string achievementsSceneName = "Achievements";  // leave empty if using panel
    [SerializeField] private string settingsSceneName = "Settings";          // leave empty if using panel

    private void Awake()
    {
        if (SaveSystem.Data == null) SaveSystem.Load();
    }

    private void Start()
    {
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

        if (achievementsPanel) achievementsPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    private void OnStartClicked()
    {
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
        if (achievementsPanel) { achievementsPanel.SetActive(true); return; }
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
        if (settingsPanel) { settingsPanel.SetActive(true); return; }
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

    // Optional close hooks
    public void CloseAchievements() { if (achievementsPanel) achievementsPanel.SetActive(false); }
    public void CloseSettings()     { if (settingsPanel)     settingsPanel.SetActive(false); }
}
