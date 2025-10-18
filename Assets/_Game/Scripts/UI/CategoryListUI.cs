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
        // --- Validate references ---
        if (!gridParent)
        {
            Debug.LogError("[CategoryListUI] gridParent not set. Assign ScrollView/Viewport/Content.");
            return;
        }

        // Ensure Save + DB are loaded (so playing this scene directly works)
        if (SaveSystem.Data == null) SaveSystem.Load();
        if (QuestionDB.Banks == null || QuestionDB.Banks.Count == 0)
        {
            Debug.Log("[CategoryListUI] Banks empty → loading from Resources/QuestionBanks …");
            QuestionDB.LoadAllFromResources();
        }
        Debug.Log($"[CategoryListUI] BankCount={QuestionDB.Banks.Count}");

        // Option B: Clear only if you want the script to own the list
        if (clearOnStart)
        {
            foreach (Transform c in gridParent) Destroy(c.gameObject);
        }

        // If no prefab assigned, either keep editor items or bail gracefully
        if (categoryButtonPrefab == null)
        {
            if (clearOnStart)
                Debug.LogWarning("[CategoryListUI] No prefab assigned and you cleared the grid, so nothing will spawn.");
            return;
        }

        // Show a dummy if no categories loaded (helps diagnose)
        if (QuestionDB.Banks.Count == 0)
        {
            var dummy = Instantiate(categoryButtonPrefab, gridParent);
            FillButton(dummy, "none", "No Categories", 0f);
            return;
        }

        // Spawn one button per category with REAL percent
        foreach (var bank in QuestionDB.Banks.Values)
        {
            var btn = Instantiate(categoryButtonPrefab, gridParent);

            // Compute real completion percentage from SaveSystem
            int totalQs = (bank.questions != null) ? bank.questions.Count : 0;
            float percent = SaveSystem.GetPercent(bank.categoryId, totalQs);

            FillButton(btn, bank.categoryId, bank.categoryName, percent);
        }
    }

    /// <summary>
    /// Fills the button UI and wires click → Quiz scene.
    /// Works whether the prefab has CategoryButtonHook or just TMP children.
    /// </summary>
    private void FillButton(Button btn, string categoryId, string displayName, float percent)
    {
        // Prefer the dedicated hook if present
        var hook = btn.GetComponent<CategoryButtonHook>();
        if (!hook) hook = btn.gameObject.AddComponent<CategoryButtonHook>();

        // Auto-find TMP refs if not assigned on the prefab
        if (!hook.categoryName || !hook.percentText)
        {
            var tmps = btn.GetComponentsInChildren<TMP_Text>(true);
            if (!hook.categoryName) hook.categoryName = tmps.FirstOrDefault(t => t.name == "CategoryName");
            if (!hook.percentText)  hook.percentText  = tmps.FirstOrDefault(t => t.name == "PercentText");
        }

        hook.Init(categoryId, displayName, percent);
    }

    // Optional: hook this to a Back button in the Category scene
    public void BackToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
