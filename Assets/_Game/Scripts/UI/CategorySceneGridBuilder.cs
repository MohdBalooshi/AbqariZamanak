using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CategorySceneGridBuilder : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CategoryTypesCatalog catalog;

    [Header("Scroll View")]
    [SerializeField] private ScrollRect scrollRect;     // ScrollView GameObject with ScrollRect
    [SerializeField] private RectTransform content;      // ScrollView/Viewport/Content

    [Header("Prefabs")]
    [SerializeField] private GameObject categoryItemPrefab; // Category card prefab (with CategoryItemUI)

    [Header("Destination Scene")]
    [SerializeField] private string destinationSceneName = "LevelSelect"; // or "Quiz"

    [Header("Grid Layout (3 per row)")]
    [SerializeField] private int columns = 3;
    [SerializeField] private Vector2 cellSize = new Vector2(330, 200);
    [SerializeField] private Vector2 spacing = new Vector2(16, 16);
    [SerializeField] private RectOffset gridPadding; // set in Inspector; default in Awake if null

    [Header("Header Style")]
    [SerializeField] private int headerFontSize = 46;
    [SerializeField] private Color headerColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    [SerializeField] private float headerTopBottom = 16f;

    private IReadOnlyDictionary<string, CategoryBank> banks;

    private void Awake()
    {
        if (gridPadding == null)
            gridPadding = new RectOffset(24, 24, 12, 24);

        if (SaveSystem.Data == null)
            SaveSystem.Load();
    }

    private void Start()
    {
        if (!ValidateInspector()) return;

        // Load banks
        if (QuestionDB.Banks == null || QuestionDB.Banks.Count == 0)
            QuestionDB.LoadAllFromResources();
        banks = QuestionDB.Banks;

        // Clear content
        foreach (Transform t in content) Destroy(t.gameObject);

        // Build sections
        if (catalog.types != null)
        {
            foreach (var typeAsset in catalog.types)
            {
                if (!typeAsset) continue;
                BuildTypeHeader(typeAsset);
                BuildTypeGrid(typeAsset);
            }
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f; // start at top
    }

    private bool ValidateInspector()
    {
        if (!scrollRect || !content)
        {
            Debug.LogError("[CategorySceneGridBuilder] Assign ScrollRect and Content.");
            return false;
        }
        if (!catalog)
        {
            Debug.LogError("[CategorySceneGridBuilder] Assign CategoryTypesCatalog.");
            return false;
        }
        if (!categoryItemPrefab)
        {
            Debug.LogError("[CategorySceneGridBuilder] Assign CategoryItem prefab.");
            return false;
        }
        return true;
    }

    // --- Builders ---

    private void BuildTypeHeader(CategoryTypeAsset typeAsset)
    {
        var headerGO = new GameObject(
            $"Header_{(string.IsNullOrEmpty(typeAsset.displayName) ? typeAsset.typeId : typeAsset.displayName)}",
            typeof(RectTransform),
            typeof(LayoutElement)
        );
        headerGO.transform.SetParent(content, false);

        var rt = headerGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(headerGO.transform, false);

        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0, 1);
        trt.anchorMax = new Vector2(1, 1);
        trt.pivot     = new Vector2(0.5f, 1);
        trt.offsetMin = new Vector2(24, -headerTopBottom);
        trt.offsetMax = new Vector2(-24, headerTopBottom);

        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = string.IsNullOrEmpty(typeAsset.displayName) ? typeAsset.typeId : typeAsset.displayName;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.fontSize = headerFontSize;
        tmp.color = headerColor;
        tmp.textWrappingMode = TextWrappingModes.Normal; // replaces obsolete enableWordWrapping

        // Reserve vertical space so parent layout can stack sections
        var le = headerGO.GetComponent<LayoutElement>();
        le.minHeight       = headerFontSize + (headerTopBottom * 2f);
        le.preferredHeight = headerFontSize + (headerTopBottom * 2f);
        le.flexibleHeight  = 0;
    }

    private void BuildTypeGrid(CategoryTypeAsset typeAsset)
    {
        var gridGO = new GameObject(
            $"Grid_{typeAsset.typeId}",
            typeof(RectTransform),
            typeof(GridLayoutGroup),
            typeof(ContentSizeFitter)
        );
        gridGO.transform.SetParent(content, false);

        var rt = gridGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var gg = gridGO.GetComponent<GridLayoutGroup>();
        gg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gg.constraintCount = Mathf.Max(1, columns); // 3 per row
        gg.cellSize = cellSize;
        gg.spacing  = spacing;
        gg.padding  = gridPadding;
        gg.childAlignment = TextAnchor.UpperCenter;

        var fitter = gridGO.GetComponent<ContentSizeFitter>();
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        if (typeAsset.categoryIds == null || typeAsset.categoryIds.Count == 0) return;

        foreach (var catId in typeAsset.categoryIds)
        {
            var item = Object.Instantiate(categoryItemPrefab, rt);
            item.name = $"Category_{catId}";

            // Ensure only card clicks work (not empty space)
            var img = item.GetComponent<Image>();
            if (img) img.raycastTarget = true; // the card
            // For child graphics (texts/images), set their Raycast Target OFF in the prefab

            var ui = item.GetComponent<CategoryItemUI>();
            if (!ui)
            {
                Debug.LogWarning($"[CategorySceneGridBuilder] CategoryItem prefab missing CategoryItemUI (catId={catId}).");
                continue;
            }

            CategoryBank bank = null;
            if (banks != null) banks.TryGetValue(catId, out bank);
            ui.Bind(catId, bank, destinationSceneName);
        }
    }
}
