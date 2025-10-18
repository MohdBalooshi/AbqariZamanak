using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class CategoryListUI : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private Transform gridParent;          // ScrollView/Viewport/Content OR any container
    [SerializeField] private Button categoryButtonPrefab;   // Prefab with Button + CategoryButtonHook

    [Header("Behavior")]
    [SerializeField] private bool clearOnStart = true;

    private void Start()
    {
        if (!gridParent)
        {
            Debug.LogError("[CategoryListUI] gridParent not set.");
            return;
        }

        if (SaveSystem.Data == null) SaveSystem.Load();
        if (QuestionDB.Banks == null || QuestionDB.Banks.Count == 0)
        {
            Debug.Log("[CategoryListUI] Loading banks from Resources/QuestionBanks …");
            QuestionDB.LoadAllFromResources();
        }

        if (clearOnStart)
        {
            foreach (Transform c in gridParent) Destroy(c.gameObject);
        }

        if (categoryButtonPrefab == null)
        {
            Debug.LogError("[CategoryListUI] categoryButtonPrefab not assigned.");
            return;
        }

        if (QuestionDB.Banks.Count == 0)
        {
            var dummy = Instantiate(categoryButtonPrefab, gridParent);
            var hook = EnsureHook(dummy);
            hook.Init("none", "No Categories", 0f);
            return;
        }

        foreach (var bank in QuestionDB.Banks.Values)
        {
            var btn = Instantiate(categoryButtonPrefab, gridParent);
            var hook = EnsureHook(btn);

            int totalQs = 0;
            if (bank.levels != null && bank.levels.Count > 0)
                foreach (var lvl in bank.levels) totalQs += (lvl.questions != null ? lvl.questions.Count : 0);
            else
                totalQs = bank.questions != null ? bank.questions.Count : 0;

            float percent = SaveSystem.GetPercent(bank.categoryId, totalQs);

            // Category name fallback
            string displayName = string.IsNullOrEmpty(bank.categoryName) ? bank.categoryId : bank.categoryName;

            hook.Init(bank.categoryId, displayName, percent);
        }

        Debug.Log($"[CategoryListUI] Spawned {QuestionDB.Banks.Count} category buttons.");
    }

    private CategoryButtonHook EnsureHook(Button btn)
    {
        var hook = btn.GetComponent<CategoryButtonHook>();
        if (!hook) hook = btn.gameObject.AddComponent<CategoryButtonHook>();

        // Auto-wire text refs if missing
        if (!hook.categoryName || !hook.percentText)
        {
            var tmps = btn.GetComponentsInChildren<TMP_Text>(true);
            if (!hook.categoryName) hook.categoryName = tmps.FirstOrDefault(t => t.name == "CategoryName") ?? tmps.FirstOrDefault();
            if (!hook.percentText)
            {
                // Try to find a child named PercentText; otherwise leave null (optional UI)
                hook.percentText = tmps.FirstOrDefault(t => t.name == "PercentText");
            }
        }

        return hook;
    }
}
