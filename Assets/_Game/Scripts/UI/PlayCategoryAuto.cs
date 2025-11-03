using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class PlayCategoryAuto : MonoBehaviour
{
    [Header("Category Data")]
    [SerializeField] private string categoryId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [Range(0,100)] [SerializeField] private float percent;

    [Header("UI References")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private Image percentFill;

    [Header("Scene")]
    [SerializeField] private string levelSelectSceneName = "LevelSelect";

    private Button _btn;

    private void Reset()
    {
        _btn = GetComponent<Button>();
    }

    private void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.RemoveAllListeners();
        _btn.onClick.AddListener(Play);
    }

    private void OnEnable()
    {
        if (title)       title.text = displayName;
        if (iconImage)   iconImage.sprite = icon;
        if (percentText) percentText.text = $"{percent:F0}%";
        if (percentFill) percentFill.fillAmount = Mathf.Clamp01(percent / 100f);
    }

    private void Play()
    {
        if (string.IsNullOrEmpty(categoryId))
        {
            Debug.LogError("[PlayCategoryAuto] categoryId is empty.");
            return;
        }

        QuizContext.SelectedCategoryId = categoryId;

        if (!Application.CanStreamedLevelBeLoaded(levelSelectSceneName))
        {
            Debug.LogError($"[PlayCategoryAuto] Scene '{levelSelectSceneName}' not in Build Settings.");
            return;
        }
        SceneManager.LoadScene(levelSelectSceneName);
    }
}
