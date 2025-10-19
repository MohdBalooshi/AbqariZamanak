using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Main Buttons")]
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnAchievements;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnLogin;   // hidden after login
    [SerializeField] private Button btnInfo;

    [Header("Profile UI (shown after login)")]
    [SerializeField] private GameObject playerNameChip; // container object (e.g., a small panel/button)
    [SerializeField] private TMP_Text playerNameText;   // text inside the chip
    [SerializeField] private bool chipOpensLoginOnClick = true; // allow changing name

    [Header("Info Panel")]
    [SerializeField] private GameObject infoPanel;      // has CanvasGroup
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Button infoBGButton;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private bool useScaleIn = true;

    [Header("Login Panel")]
    [SerializeField] private GameObject loginPanel;     // has CanvasGroup
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button loginConfirmButton;
    [SerializeField] private Button loginCancelButton;
    [SerializeField] private Button loginBGButton;

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;  // has CanvasGroup
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle vibrateToggle;
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private Button settingsApplyButton;
    [SerializeField] private Button settingsCloseButton;
    [SerializeField] private Button settingsBGButton;

    [Header("Scene Names")]
    [SerializeField] private string categorySceneName = "CategoryScene";
    [SerializeField] private string achievementsSceneName = "Achievements";

    private Coroutine infoRoutine, loginRoutine, settingsRoutine;

    private void Awake()
    {
        if (SaveSystem.Data == null) SaveSystem.Load();
    }

    private void Start()
    {
        // Main nav buttons (with login gate)
        if (btnStart)        { btnStart.onClick.RemoveAllListeners();        btnStart.onClick.AddListener(OnStartGate); }
        if (btnAchievements) { btnAchievements.onClick.RemoveAllListeners(); btnAchievements.onClick.AddListener(OnAchievementsGate); }
        if (btnSettings)     { btnSettings.onClick.RemoveAllListeners();     btnSettings.onClick.AddListener(OpenSettings); }
        if (btnLogin)        { btnLogin.onClick.RemoveAllListeners();        btnLogin.onClick.AddListener(OpenLogin); }
        if (btnInfo)         { btnInfo.onClick.RemoveAllListeners();         btnInfo.onClick.AddListener(ToggleInfo); }

        // Optional: name chip click opens login to change name
        if (playerNameChip && chipOpensLoginOnClick)
        {
            var chipBtn = playerNameChip.GetComponent<Button>();
            if (chipBtn)
            {
                chipBtn.onClick.RemoveAllListeners();
                chipBtn.onClick.AddListener(OpenLogin);
            }
        }

        // BG closers
        if (infoBGButton)       { infoBGButton.onClick.RemoveAllListeners();       infoBGButton.onClick.AddListener(ToggleInfo); }
        if (loginConfirmButton) { loginConfirmButton.onClick.RemoveAllListeners(); loginConfirmButton.onClick.AddListener(ConfirmLogin); }
        if (loginCancelButton)  { loginCancelButton.onClick.RemoveAllListeners();  loginCancelButton.onClick.AddListener(CloseLogin); }
        if (loginBGButton)      { loginBGButton.onClick.RemoveAllListeners();      loginBGButton.onClick.AddListener(CloseLogin); }
        if (settingsApplyButton){ settingsApplyButton.onClick.RemoveAllListeners(); settingsApplyButton.onClick.AddListener(ApplySettings); }
        if (settingsCloseButton){ settingsCloseButton.onClick.RemoveAllListeners(); settingsCloseButton.onClick.AddListener(CloseSettings); }
        if (settingsBGButton)   { settingsBGButton.onClick.RemoveAllListeners();   settingsBGButton.onClick.AddListener(CloseSettings); }

        // Panels default
        if (infoPanel)     infoPanel.SetActive(false);
        if (loginPanel)    loginPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);

        // Prefill login & settings
        if (nicknameInput) nicknameInput.text = SaveSystem.GetPlayerName();

        var s = SaveSystem.GetSettings();
        if (musicSlider)    musicSlider.value = s.musicVolume;
        if (sfxSlider)      sfxSlider.value   = s.sfxVolume;
        if (vibrateToggle)  vibrateToggle.isOn = s.vibrate;
        if (languageDropdown)
        {
            int index = languageDropdown.options.FindIndex(o => o.text.ToLower() == s.language.ToLower());
            languageDropdown.value = index >= 0 ? index : 0;
            languageDropdown.RefreshShownValue();
        }

        // Apply gate + profile UI
        bool loggedIn = SaveSystem.IsLoggedIn();
        SetGateInteractivity(loggedIn);
        RefreshProfileUI();

        // First-run: show login immediately if not logged in
        if (!loggedIn) OpenLogin();
    }

    // ---------------- Gate + Profile UI ----------------
    private void SetGateInteractivity(bool allow)
    {
        if (btnStart)        btnStart.interactable        = allow;
        if (btnAchievements) btnAchievements.interactable = allow;

        // If you want Settings locked too, uncomment:
        // if (btnSettings) btnSettings.interactable = allow;
    }

    private void RefreshProfileUI()
    {
        bool logged = SaveSystem.IsLoggedIn();
        string name = SaveSystem.GetPlayerName();

        if (btnLogin) btnLogin.gameObject.SetActive(!logged);

        if (playerNameChip) playerNameChip.SetActive(logged);
        if (playerNameText) playerNameText.text = string.IsNullOrEmpty(name) ? "Player" : name;
    }

    // ---------------- Navigation (with gate) ----------------
    private void OnStartGate()
    {
        if (!SaveSystem.IsLoggedIn()) { OpenLogin(); return; }
        LoadScene(categorySceneName);
    }
    private void OnAchievementsGate()
    {
        if (!SaveSystem.IsLoggedIn()) { OpenLogin(); return; }
        LoadScene(achievementsSceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[MainMenu] Scene '{sceneName}' not in Build Settings.");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    // ---------------- Info ----------------
    public void ToggleInfo()
    {
        if (!infoPanel || !infoText) return;
        var cg = EnsureCG(infoPanel);

        if (!infoPanel.activeSelf)
        {
            infoText.text = "Welcome to عبقري زمانك — a fun, fast quiz that challenges your knowledge across many categories!";
            infoPanel.SetActive(true);
            infoPanel.transform.localScale = useScaleIn ? Vector3.one * 0.92f : Vector3.one;
            if (infoRoutine != null) StopCoroutine(infoRoutine);
            infoRoutine = StartCoroutine(Fade(cg, 0f, 1f, fadeDuration, infoPanel.transform, useScaleIn ? 0.92f : 1f, 1f));
        }
        else
        {
            if (infoRoutine != null) StopCoroutine(infoRoutine);
            infoRoutine = StartCoroutine(FadeOutDisable(infoPanel, cg, fadeDuration, useScaleIn));
        }
    }

    // ---------------- Login ----------------
    public void OpenLogin()
    {
        if (!loginPanel) return;
        if (nicknameInput) nicknameInput.text = SaveSystem.GetPlayerName(); // refresh with current name
        var cg = EnsureCG(loginPanel);
        loginPanel.SetActive(true);
        loginPanel.transform.localScale = useScaleIn ? Vector3.one * 0.92f : Vector3.one;
        if (loginRoutine != null) StopCoroutine(loginRoutine);
        loginRoutine = StartCoroutine(Fade(cg, 0f, 1f, fadeDuration, loginPanel.transform, useScaleIn ? 0.92f : 1f, 1f));
    }

    public void CloseLogin()
    {
        if (!loginPanel) return;
        var cg = EnsureCG(loginPanel);
        if (loginRoutine != null) StopCoroutine(loginRoutine);
        loginRoutine = StartCoroutine(FadeOutDisable(loginPanel, cg, fadeDuration, useScaleIn));
    }

    public void ConfirmLogin()
    {
        var name = nicknameInput ? nicknameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name)) name = "Player";
        SaveSystem.SetPlayerName(name);

        // Enable navigation + update UI
        SetGateInteractivity(true);
        RefreshProfileUI();

        Debug.Log($"[MainMenu] Player name set to '{name}'.");
        CloseLogin();
    }

    // ---------------- Settings ----------------
    public void OpenSettings()
    {
        if (!settingsPanel) return;

        var s = SaveSystem.GetSettings();
        if (musicSlider)    musicSlider.value = s.musicVolume;
        if (sfxSlider)      sfxSlider.value   = s.sfxVolume;
        if (vibrateToggle)  vibrateToggle.isOn = s.vibrate;
        if (languageDropdown)
        {
            int index = languageDropdown.options.FindIndex(o => o.text.ToLower() == s.language.ToLower());
            languageDropdown.value = index >= 0 ? index : 0;
            languageDropdown.RefreshShownValue();
        }

        var cg = EnsureCG(settingsPanel);
        settingsPanel.SetActive(true);
        settingsPanel.transform.localScale = useScaleIn ? Vector3.one * 0.92f : Vector3.one;
        if (settingsRoutine != null) StopCoroutine(settingsRoutine);
        settingsRoutine = StartCoroutine(Fade(cg, 0f, 1f, fadeDuration, settingsPanel.transform, useScaleIn ? 0.92f : 1f, 1f));
    }

    public void CloseSettings()
    {
        if (!settingsPanel) return;
        var cg = EnsureCG(settingsPanel);
        if (settingsRoutine != null) StopCoroutine(settingsRoutine);
        settingsRoutine = StartCoroutine(FadeOutDisable(settingsPanel, cg, fadeDuration, useScaleIn));
    }

    public void ApplySettings()
    {
        var s = SaveSystem.GetSettings();
        if (musicSlider)   s.musicVolume = musicSlider.value;
        if (sfxSlider)     s.sfxVolume   = sfxSlider.value;
        if (vibrateToggle) s.vibrate     = vibrateToggle.isOn;
        if (languageDropdown) s.language = languageDropdown.options[languageDropdown.value].text;

        SaveSystem.SetSettings(s);
        Debug.Log($"[MainMenu] Settings saved: music={s.musicVolume:0.00}, sfx={s.sfxVolume:0.00}, vibrate={s.vibrate}, lang={s.language}");
        CloseSettings();
    }

    // ---------------- Fade helpers ----------------
    private static CanvasGroup EnsureCG(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (!cg) cg = go.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
        cg.interactable   = true;
        return cg;
    }

    private IEnumerator Fade(CanvasGroup cg, float a0, float a1, float dur, Transform t = null, float s0 = 1f, float s1 = 1f)
    {
        float t0 = 0f;
        cg.alpha = a0;
        if (t) t.localScale = Vector3.one * s0;

        while (t0 < dur)
        {
            t0 += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t0 / dur);
            cg.alpha = Mathf.Lerp(a0, a1, k);
            if (t) t.localScale = Vector3.one * Mathf.Lerp(s0, s1, k);
            yield return null;
        }
        cg.alpha = a1;
        if (t) t.localScale = Vector3.one * s1;
    }

    private IEnumerator FadeOutDisable(GameObject panel, CanvasGroup cg, float dur, bool scale)
    {
        float a0 = cg.alpha;
        float a1 = 0f;
        Vector3 s0 = panel.transform.localScale;
        Vector3 s1 = scale ? Vector3.one * 0.96f : s0;

        float t0 = 0f;
        while (t0 < dur)
        {
            t0 += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t0 / dur);
            cg.alpha = Mathf.Lerp(a0, a1, k);
            if (scale) panel.transform.localScale = Vector3.Lerp(s0, s1, k);
            yield return null;
        }
        cg.alpha = 0f;
        panel.SetActive(false);
    }
}
