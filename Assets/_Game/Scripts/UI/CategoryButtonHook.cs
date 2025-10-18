using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CategoryButtonHook : MonoBehaviour
{
    [Header("UI (optional)")]
    public TMP_Text categoryName;
    public TMP_Text percentText;

    [Header("Navigation")]
    [SerializeField] private string levelSelectSceneName = "LevelSelect";

    private string _categoryId;

    /// <summary>
    /// Call this right after instantiating the button.
    /// </summary>
    public void Init(string categoryId, string displayName, float percent)
    {
        _categoryId = categoryId;

        if (categoryName) categoryName.text = displayName;
        if (percentText)  percentText.text  = $"{percent:0}%";

        var btn = GetComponent<Button>();
        if (!btn)
        {
            Debug.LogError("[CategoryButtonHook] No Button component on this object.");
            return;
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClicked);

        // For diagnosis
        Debug.Log($"[CategoryButtonHook] Init: id={_categoryId}, name={displayName}, %={percent:0}");
    }

    private void OnClicked()
    {
        Debug.Log($"[CategoryButtonHook] Clicked category '{_categoryId}'. Loading '{levelSelectSceneName}'…");

        // Stash context so LevelSelect knows which category to show
        QuizContext.SelectedCategoryId = _categoryId;

        if (!Application.CanStreamedLevelBeLoaded(levelSelectSceneName))
        {
            Debug.LogError($"[CategoryButtonHook] Scene '{levelSelectSceneName}' not found in Build Settings.");
            return;
        }

        SceneManager.LoadScene(levelSelectSceneName);
    }
}
