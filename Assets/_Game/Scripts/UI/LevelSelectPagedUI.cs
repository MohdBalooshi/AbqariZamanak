using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSelectPagedUI : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private Transform gridParent;       // GridRoot
    [SerializeField] private Button levelButtonPrefab;   // LevelButton prefab (with LevelButtonHook)
    [SerializeField] private TMP_Text categoryTitle;     // Header/Title (optional)
    [SerializeField] private Button btnPrev;             // PageBar/Btn_Prev
    [SerializeField] private Button btnNext;             // PageBar/Btn_Next
    [SerializeField] private TMP_Text pageText;          // PageBar/PageText

    [Header("Config")]
    [SerializeField] private int levelsPerPage = 20;      // 4 x 5 grid
    [SerializeField] private int displayLevelCount = 100; // show 5 pages x 20, even if data has fewer
    [SerializeField] private bool lockIfNoContent = true;
    [SerializeField] private string categoryIdOverride;   // leave empty to use QuizContext

    private string categoryId;
    private int currentPage = 0; // zero-based
    private int totalLevelsInData = 0;
    private int totalPages = 0;

    private void Start()
    {
        categoryId = string.IsNullOrEmpty(categoryIdOverride) ? QuizContext.SelectedCategoryId : categoryIdOverride;
        if (string.IsNullOrEmpty(categoryId))
        {
            Debug.LogError("[LevelSelectPagedUI] No categoryId found. Set QuizContext.SelectedCategoryId before loading this scene.");
            return;
        }

        if (QuestionDB.Banks == null || QuestionDB.Banks.Count == 0) QuestionDB.LoadAllFromResources();
        if (SaveSystem.Data == null) SaveSystem.Load();

        var bank = QuestionDB.Banks[categoryId];

        totalLevelsInData = (bank.levels != null && bank.levels.Count > 0) ? bank.levels.Count : 1;
        int totalLevelsForDisplay = Mathf.Max(totalLevelsInData, displayLevelCount);
        totalPages = Mathf.CeilToInt(totalLevelsForDisplay / (float)levelsPerPage);

        if (categoryTitle) categoryTitle.text = bank.categoryName;

        if (btnPrev) btnPrev.onClick.AddListener(OnPrev);
        if (btnNext) btnNext.onClick.AddListener(OnNext);

        currentPage = Mathf.Clamp(currentPage, 0, Mathf.Max(0, totalPages - 1));
        RebuildPage();
    }

    private void OnPrev()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RebuildPage();
        }
    }

    private void OnNext()
    {
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            RebuildPage();
        }
    }

    private void RebuildPage()
    {
        foreach (Transform c in gridParent) Destroy(c.gameObject);

        int totalLevelsForDisplay = totalPages * levelsPerPage;
        int startLevelIndex = currentPage * levelsPerPage + 1;
        int endLevelIndex   = Mathf.Min(startLevelIndex + levelsPerPage - 1, totalLevelsForDisplay);

        int unlockedMax = SaveSystem.GetUnlockedLevelCount(categoryId);

        for (int levelIndex = startLevelIndex; levelIndex <= endLevelIndex; levelIndex++)
        {
            var btn = Instantiate(levelButtonPrefab, gridParent);
            var hook = btn.GetComponent<LevelButtonHook>();
            if (!hook) hook = btn.gameObject.AddComponent<LevelButtonHook>();

            if (!hook.label)
            {
                var t = btn.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
                if (t) hook.label = t;
            }
            if (!hook.lockIcon)
            {
                var icons = btn.GetComponentsInChildren<Transform>(true);
                foreach (var tr in icons)
                {
                    if (tr.name.ToLower().Contains("lock"))
                    {
                        hook.lockIcon = tr.gameObject;
                        break;
                    }
                }
            }

            bool hasContent = LevelExistsInData(categoryId, levelIndex);
            bool unlocked = (levelIndex <= unlockedMax) && (!lockIfNoContent || hasContent);

            hook.Init(categoryId, levelIndex, unlocked);

            if (!hasContent && lockIfNoContent)
            {
                var button = btn.GetComponent<Button>();
                if (button) button.interactable = false;
            }
        }

        if (pageText) pageText.text = $"Page {currentPage + 1}/{Mathf.Max(totalPages, 1)}";
        if (btnPrev) btnPrev.interactable = (currentPage > 0);
        if (btnNext) btnNext.interactable = (currentPage < totalPages - 1);
    }

    private bool LevelExistsInData(string catId, int levelIndex)
    {
        if (!QuestionDB.Banks.TryGetValue(catId, out var bank)) return false;

        if (bank.levels != null && bank.levels.Count > 0)
            return bank.levels.Any(l => l.levelIndex == levelIndex && l.questions != null && l.questions.Count > 0);

        // legacy single-level bank
        return (levelIndex == 1) && (bank.questions != null && bank.questions.Count > 0);
    }

    public void Refresh() => RebuildPage();

    public void BackToCategories()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("CategoryScene");
    }
}
