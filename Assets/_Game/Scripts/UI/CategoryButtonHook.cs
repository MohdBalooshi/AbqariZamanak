using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CategoryButtonHook : MonoBehaviour
{
    [Header("Optional UI")]
    public TMP_Text categoryName;
    public TMP_Text percentText;

    private string _categoryId;

    public void Init(string categoryId, string displayName, float percent)
    {
        _categoryId = categoryId;

        if (categoryName) categoryName.text = displayName;
        if (percentText)  percentText.text  = $"{percent:0}%";

        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        Debug.Log($"[CategoryButton] '{_categoryId}' clicked → load Quiz");
        QuizContext.SelectedCategoryId = _categoryId;
        SceneManager.LoadScene("Quiz");
    }
}
