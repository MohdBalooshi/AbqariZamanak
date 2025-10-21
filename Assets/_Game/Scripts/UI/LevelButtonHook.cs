using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

public class LevelButtonHook : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text label;                  // "Level 1"
    public GameObject lockIcon;
    public GameObject completedIcon;
    [SerializeField] private Image progressFill; // type: Filled, Horizontal, FillAmount 0..1
    [SerializeField] private TMP_Text progressText; // "45%"

    [Header("Navigation")]
    [SerializeField] private string quizSceneName = "Quiz";

    [Header("Guards")]
    [SerializeField] private bool requireContentToEnter = true;

    [Header("Economy")]
    [SerializeField] private EconomyConfig economy;

    private string _categoryId;
    private int _levelIndex;
    private bool _unlocked;

    public void Init(string categoryId, int levelIndex, bool unlocked)
    {
        _categoryId = categoryId;
        _levelIndex = levelIndex;
        _unlocked = unlocked;

        if (label) label.text = $"Level {_levelIndex}";

        var btn = GetComponent<Button>();
        var img = GetComponent<Image>();
        if (!btn || !img)
        {
            Debug.LogError("[LevelButtonHook] Missing Button or Image on root.");
            return;
        }
        if (btn.targetGraphic == null) btn.targetGraphic = img;
        img.raycastTarget = true;

        // Completion & progress
        bool isComplete = SaveSystem.IsLevelComplete(_categoryId, _levelIndex);
        if (lockIcon)      lockIcon.SetActive(!_unlocked);
        if (completedIcon) completedIcon.SetActive(isComplete);

        // Progress bar
        float pct = SaveSystem.GetLevelPercent(_categoryId, _levelIndex);
        if (progressFill)  progressFill.fillAmount = Mathf.Clamp01(pct / 100f);
        if (progressText)  progressText.text = $"{pct:0}%";

        btn.interactable = _unlocked && !isComplete;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (!_unlocked) { Debug.LogWarning("Level locked."); return; }
        if (SaveSystem.IsLevelComplete(_categoryId, _levelIndex))
        {
            Debug.LogWarning("Level already 100% complete. Entry disabled.");
            return;
        }
        if (requireContentToEnter && !LevelHasContent(_categoryId, _levelIndex))
        {
            Debug.LogWarning($"Level {_levelIndex} has no questions; blocking entry.");
            return;
        }

        int cost = economy ? economy.levelEntryCost : 0;
        if (cost > 0)
        {
            if (!SaveSystem.HasCoins(cost) || !SaveSystem.TrySpend(cost))
            {
                CoinsGatePopup.Show(cost);
                return;
            }
        }

        QuizContext.SelectedCategoryId = _categoryId;
        QuizContext.SelectedLevelIndex = _levelIndex;

        if (!Application.CanStreamedLevelBeLoaded(quizSceneName))
        {
            Debug.LogError($"Scene '{quizSceneName}' not found in Build Settings.");
            return;
        }
        SceneManager.LoadScene(quizSceneName);
    }

    private bool LevelHasContent(string catId, int levelIndex)
    {
        if (QuestionDB.Banks == null || QuestionDB.Banks.Count == 0) QuestionDB.LoadAllFromResources();
        if (!QuestionDB.Banks.TryGetValue(catId, out var bank)) return false;

        if (bank.levels != null && bank.levels.Count > 0)
        {
            var lvl = bank.levels.FirstOrDefault(l => l.levelIndex == levelIndex);
            return lvl != null && lvl.questions != null && lvl.questions.Count > 0;
        }
        return (levelIndex == 1) && (bank.questions != null && bank.questions.Count > 0);
    }
}
