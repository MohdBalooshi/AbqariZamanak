using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds the Category Scene dynamically:
/// Displays each Type header (like "General", "Anime")
/// followed by a grid of Category cards underneath.
/// </summary>
public class CategorySceneListBuilder : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CategoryTypesCatalog catalog;

    [Header("Scroll View")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content; // ScrollRect.content

    [Header("Prefabs")]
    [SerializeField] private GameObject typeSectionPrefab;   // Header + Grid
    [SerializeField] private GameObject categoryItemPrefab;  // Category Card

    [Header("Scenes")]
    [SerializeField] private string levelSelectSceneName = "LevelSelect";

    [Header("Grid Config (Optional Override)")]
    [SerializeField] private int columns = 2;
    [SerializeField] private Vector2 cellSize = new Vector2(480, 220);
    [SerializeField] private Vector2 spacing = new Vector2(16, 16);
    [SerializeField] private RectOffset padding;

private IReadOnlyDictionary<string, CategoryBank> banks;

    private void Awake()
    {
        if (SaveSystem.Data == null)
            SaveSystem.Load();
    }

    private void Start()
    {
        if (!scrollRect || !content)
        {
            Debug.LogError("[CategorySceneListBuilder] Please assign ScrollRect and Content in the inspector.");
            return;
        }

        if (!catalog || catalog.types == null || catalog.types.Count == 0)
        {
            Debug.LogError("[CategorySceneListBuilder] Catalog is empty or not assigned.");
            return;
        }

        if (!typeSectionPrefab || !categoryItemPrefab)
        {
            Debug.LogError("[CategorySceneListBuilder] Prefabs not assigned.");
            return;
        }

        // Load all question banks if not yet loaded
        if (QuestionDB.Banks == null || QuestionDB.Banks.Count == 0)
            QuestionDB.LoadAllFromResources();

        banks = QuestionDB.Banks;

        // Clear existing content
        foreach (Transform t in content)
            Destroy(t.gameObject);

        // Build each type section
        foreach (var typeAsset in catalog.types)
        {
            if (typeAsset == null) continue;

            // Instantiate the Type Section
            GameObject section = Instantiate(typeSectionPrefab, content);
            section.name = $"TypeSection_{typeAsset.displayName ?? typeAsset.typeId}";

            // Find header text and grid root
            TMP_Text header = section.GetComponentInChildren<TMP_Text>(true);
            if (header)
                header.text = string.IsNullOrEmpty(typeAsset.displayName)
                    ? typeAsset.typeId
                    : typeAsset.displayName;

            RectTransform gridRoot = null;
            var grids = section.GetComponentsInChildren<GridLayoutGroup>(true);
            if (grids != null && grids.Length > 0)
                gridRoot = grids[0].transform as RectTransform;

            // Enforce grid config (optional override)
            if (gridRoot)
            {
                GridLayoutGroup gg = gridRoot.GetComponent<GridLayoutGroup>();
                if (gg)
                {
                    gg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    gg.constraintCount = Mathf.Max(1, columns);
                    gg.cellSize = cellSize;
                    gg.spacing = spacing;
                    if (padding != null)
                        gg.padding = padding;
                }
            }

            // Add category cards under this type
            if (typeAsset.categoryIds != null)
            {
                foreach (string catId in typeAsset.categoryIds)
                {
                    GameObject item = Instantiate(categoryItemPrefab, gridRoot ? gridRoot : section.transform);
                    item.name = $"Category_{catId}";

                    CategoryItemUI ui = item.GetComponent<CategoryItemUI>();
                    if (!ui)
                    {
                        Debug.LogWarning($"[CategorySceneListBuilder] CategoryItem prefab missing CategoryItemUI: {catId}");
                        continue;
                    }

                    banks.TryGetValue(catId, out CategoryBank bank);
                    ui.Bind(catId, bank, levelSelectSceneName);
                }
            }
        }

        // Force layout update and start scroll at top
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }
}
