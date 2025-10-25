using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CategoryItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text levelsText;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private Image percentFill; // Image Type = Filled (Horizontal, Origin Left)

    [Header("Destination Scene")]
    [SerializeField] private string destinationSceneName = "LevelSelect"; // or "Quiz" if you prefer

    private string categoryId;

    private void Awake()
    {
        // Ensure only the card (root) is clickable
        var btn = GetComponent<Button>();
        if (btn)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnPressed);
        }
    }

    public void Bind(string catId, CategoryBank bank, string overrideSceneIfProvided)
    {
        categoryId = catId;
        if (!string.IsNullOrEmpty(overrideSceneIfProvided))
            destinationSceneName = overrideSceneIfProvided;

        // Title
        if (titleText)
            titleText.text = bank != null ? bank.categoryName : catId;

        // Levels count
        int totalLevels = GetTotalLevels(bank);
        if (levelsText)
            levelsText.text = $"Levels: {totalLevels}";

        // Percent complete
        int totalQs = TotalQuestionCount(bank);
        float pct = (totalQs > 0) ? SaveSystem.GetPercent(catId, totalQs) : 0f;

        if (percentText)  percentText.text = $"{Mathf.RoundToInt(pct)}%";
        if (percentFill)
        {
            percentFill.type = Image.Type.Filled;
            percentFill.fillMethod = Image.FillMethod.Horizontal;
            percentFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            percentFill.fillAmount = Mathf.Clamp01(pct / 100f);
        }
    }

    private void OnPressed()
    {
        if (string.IsNullOrEmpty(categoryId)) return;

        QuizContext.SelectedCategoryId = categoryId;

        if (!Application.CanStreamedLevelBeLoaded(destinationSceneName))
        {
            Debug.LogError($"[CategoryItemUI] Scene '{destinationSceneName}' not in Build Settings.");
            return;
        }
        SceneManager.LoadScene(destinationSceneName);
    }

    // --- helpers ---
    private static int TotalQuestionCount(CategoryBank b)
    {
        if (b == null) return 0;
        if (b.levels != null && b.levels.Count > 0)
        {
            int sum = 0;
            foreach (var l in b.levels) sum += (l.questions != null ? l.questions.Count : 0);
            return sum;
        }
        return (b.questions != null) ? b.questions.Count : 0;
    }

    private static int GetTotalLevels(CategoryBank b)
    {
        if (b == null) return 0;
        if (b.levels != null && b.levels.Count > 0) return b.levels.Count;
        return (b.questions != null && b.questions.Count > 0) ? 1 : 0;
    }
}
