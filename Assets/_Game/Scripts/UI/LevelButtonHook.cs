using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelButtonHook : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text label;
    public GameObject lockIcon;

    private string _categoryId;
    private int _levelIndex;
    private bool _unlocked;

    public void Init(string categoryId, int levelIndex, bool unlocked)
    {
        _categoryId = categoryId;
        _levelIndex = levelIndex;
        _unlocked = unlocked;

        if (label) label.text = $"Level {_levelIndex}";
        if (lockIcon) lockIcon.SetActive(!unlocked);

        var btn = GetComponent<Button>();
        btn.interactable = unlocked;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (!_unlocked) return;
        QuizContext.SelectedCategoryId = _categoryId;
        QuizContext.SelectedLevelIndex = _levelIndex;
        SceneManager.LoadScene("Quiz"); // make sure Quiz is in Build Settings
    }
}
