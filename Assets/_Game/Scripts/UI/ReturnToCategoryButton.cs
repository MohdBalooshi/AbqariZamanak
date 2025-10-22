using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class ReturnToCategoryButton : MonoBehaviour
{
    [SerializeField] private string categorySceneName = "CategoryScene"; // default

    private void Awake()
    {
        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(ReturnToCategories);
    }

    private void ReturnToCategories()
    {
        // Optional safety: check if scene is in build settings
        if (Application.CanStreamedLevelBeLoaded(categorySceneName))
        {
            SceneManager.LoadScene(categorySceneName);
        }
        else
        {
            Debug.LogError($"[ReturnToCategoryButton] Scene '{categorySceneName}' not found in Build Settings!");
        }
    }
}
