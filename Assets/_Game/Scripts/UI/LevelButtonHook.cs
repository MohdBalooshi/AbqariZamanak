using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

public class LevelButtonHook : MonoBehaviour
{
    [Header("UI (optional)")]
    public TMP_Text label;
    public GameObject lockIcon;        // shown if locked
    public GameObject completedIcon;   // optional “✔” or ribbon

    // Hardcode to avoid prefab/Inspector drift
    private const string QUIZ_SCENE_NAME = "Quiz";

    [Header("Guards")]
    [SerializeField] private bool requireContentToEnter = true;

    private string _categoryId;
    private int _levelIndex;
    private bool _unlocked;

    public void Init(string categoryId, int levelIndex, bool unlocked)
    {
        _categoryId = categoryId;
        _levelIndex = levelIndex;
        _unlocked   = unlocked;

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

        bool isComplete = SaveSystem.IsLevelComplete(_categoryId, _levelIndex);

        if (lockIcon)      lockIcon.SetActive(!_unlocked);
        if (completedIcon) completedIcon.SetActive(isComplete);

        btn.interactable = _unlocked && !isComplete;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClick);

        Debug.Log($"[LevelButtonHook] Init: cat={_categoryId}, L{_levelIndex}, unlocked={_unlocked}, complete={isComplete}");
    }

    private void OnClick()
    {
        Debug.Log($"[LevelButtonHook] CLICK: cat={_categoryId}, level={_levelIndex}");

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

        QuizContext.SelectedCategoryId = _categoryId;
        QuizContext.SelectedLevelIndex = _levelIndex;

        if (!Application.CanStreamedLevelBeLoaded(QUIZ_SCENE_NAME))
        {
            Debug.LogError($"[LevelButtonHook] Scene '{QUIZ_SCENE_NAME}' not found in Build Settings.");
            return;
        }
        SceneManager.LoadScene(QUIZ_SCENE_NAME);
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
