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
    [SerializeField] private int levelsPerPage = 20;     // 4 x 5 grid
    [SerializeField] private int displayLevelCount = 100; // ← Force 100 levels (5 pages) visible even if JSON has fewer
    [SerializeField] private bool lockIfNoContent = true; // lock buttons that have no questions yet
    [SerializeField] private string categoryIdOverride;  // leave empty to use QuizContext

    private string categoryId;
    private int currentPage = 0; // zero-based
    private int totalLevelsInData = 0;  // how many levels exist in JSON
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

        // How many levels actually exist in the data?
        totalLevelsInData = (bank.levels != null && bank.levels.Count > 0) ? bank.levels.Count : 1; // legacy fallback

        // How many levels to show in the UI?
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
        // Clear grid
        foreach (Transform c in gridParent) Destroy(c.gameObject);

        // Work out display range
        int totalLevelsForDisplay = totalPages * levelsPerPage; // consistent page count
        int startLevelIndex = currentPage * levelsPerPage + 1;  // 1-based
        int endLevelIndex   = Mathf.Min(startLevelIndex + levelsPerPage - 1, totalLevelsForDisplay);

        // Unlock state from save
        int unlockedMax = SaveSystem.GetUnlockedLevelCount(categoryId);

        // Spawn buttons for this page
        for (int levelIndex = startLevelIndex; levelIndex <= endLevelIndex; levelIndex++)
        {
            var btn = Instantiate(levelButtonPrefab, gridParent);
            var hook = btn.GetComponent<LevelButtonHook>();
            if (!hook) hook = btn.gameObject.AddComponent<LevelButtonHook>();

            // Auto-wire label/lock if prefab isn't prewired
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

            // Does this level actually have content in the JSON?
            bool hasContent = LevelExistsInData(categoryId, levelIndex);

            // Unlocked if: level <= unlockedMax AND has content (or allow click without content if you want to show a message)
            bool unlocked = (levelIndex <= unlockedMax) && (!lockIfNoContent || hasContent);

            // If you want to visually differentiate "no content yet", you can also change the label here.
            hook.Init(categoryId, levelIndex, unlocked);

            // If no content and you want to block clicks even if unlockedMax is high:
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
        {
            // explicit levels exist
            return bank.levels.Any(l => l.levelIndex == levelIndex && l.questions != null && l.questions.Count > 0);
        }
        else
        {
            // legacy: treat everything as level 1 only
            return (levelIndex == 1) && (bank.questions != null && bank.questions.Count > 0);
        }
    }

    // Optional: refresh after coming back from Quiz without reloading scene
    public void Refresh() => RebuildPage();

    public void BackToCategories()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("CategoryScene");
    }
}
