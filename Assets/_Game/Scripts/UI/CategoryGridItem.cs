using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CategoryGridItem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text title;

    [Header("Scenes")]
    [SerializeField] private string levelSelectSceneName = "LevelSelect";

    private string _categoryId;

    public void Init(string categoryId, string displayName, Sprite displayIcon)
    {
        _categoryId = categoryId;
        if (title) title.text = displayName;
        if (icon && displayIcon) icon.sprite = displayIcon;

        var btn = GetComponent<Button>();
        if (!btn) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        // Match your existing flow:
        // QuizContext.SelectedCategoryId and load LevelSelect
        QuizContext.SelectedCategoryId = _categoryId;

        if (!Application.CanStreamedLevelBeLoaded(levelSelectSceneName))
        {
            Debug.LogError($"[CategoryGridItem] Scene '{levelSelectSceneName}' not found in Build Settings.");
            return;
        }
        SceneManager.LoadScene(levelSelectSceneName);
    }
}
