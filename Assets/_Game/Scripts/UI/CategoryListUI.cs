using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class CategoryListUI : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private Transform gridParent;          // ScrollView/Viewport/Content
    [SerializeField] private Button categoryButtonPrefab;   // CategoryButton prefab (root has Button)

    [Header("Behavior")]
    [SerializeField] private bool clearOnStart = true;      // ON = script owns list. OFF = keep editor-placed items.

    private void Start()
    {
        if (!gridParent)
        {
            Debug.LogError("[CategoryListUI] gridParent not set. Assign ScrollView/Viewport/Content.");
            return;
        }
        if (QuestionDB.Banks == null || QuestionDB.Banks.Count == 0)
        {
            Debug.Log("[CategoryListUI] Banks empty → loading from Resources/QuestionBanks …");
            QuestionDB.LoadAllFromResources();
        }
        Debug.Log($"[CategoryListUI] BankCount={QuestionDB.Banks.Count}");

        if (clearOnStart)
        {
            foreach (Transform c in gridParent) Destroy(c.gameObject);
        }

        if (categoryButtonPrefab == null)
        {
            if (clearOnStart)
                Debug.LogWarning("[CategoryListUI] No prefab assigned and you cleared the grid, so nothing will spawn.");
            return;
        }

        if (QuestionDB.Banks.Count == 0)
        {
            var dummy = Instantiate(categoryButtonPrefab, gridParent);
            FillButton(dummy, "none", "No Categories", 0f);
            return;
        }

        foreach (var bank in QuestionDB.Banks.Values)
        {
            var btn = Instantiate(categoryButtonPrefab, gridParent);
            FillButton(btn, bank.categoryId, bank.categoryName, 0f);
        }
    }

    private void FillButton(Button btn, string categoryId, string displayName, float percent)
    {
        var hook = btn.GetComponent<CategoryButtonHook>();
        if (!hook) hook = btn.gameObject.AddComponent<CategoryButtonHook>();

        // Auto-find texts if not assigned on the prefab
        if (!hook.categoryName || !hook.percentText)
        {
            var tmps = btn.GetComponentsInChildren<TMP_Text>(true);
            if (!hook.categoryName) hook.categoryName = tmps.FirstOrDefault(t => t.name == "CategoryName");
            if (!hook.percentText)  hook.percentText  = tmps.FirstOrDefault(t => t.name == "PercentText");
        }

        hook.Init(categoryId, displayName, percent);
    }

    public void BackToMenu() =>
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
}
