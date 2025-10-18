using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CategoryButtonHook : MonoBehaviour
{
    public TMP_Text categoryName;
    public TMP_Text percentText;

    [SerializeField] private string levelSelectSceneName = "LevelSelect";

    private string _categoryId;

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

        Debug.Log($"[CategoryButtonHook] Init: id={_categoryId}, name={displayName}, %={percent:0}");
    }

    private void OnClicked()
    {
        Debug.Log($"[CategoryButtonHook] Clicked category '{_categoryId}'. Loading '{levelSelectSceneName}'…");
        QuizContext.SelectedCategoryId = _categoryId;

        if (!Application.CanStreamedLevelBeLoaded(levelSelectSceneName))
        {
            Debug.LogError($"[CategoryButtonHook] Scene '{levelSelectSceneName}' not found in Build Settings.");
            return;
        }
        SceneManager.LoadScene(levelSelectSceneName);
    }
}
