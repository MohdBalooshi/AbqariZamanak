using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CategoryGridController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private CategoryTypesCatalog catalog;

    [Header("View")]
    [SerializeField] private Transform contentVertical;      // Content_Vertical
    [SerializeField] private TypeSectionController sectionPrefab;
    [SerializeField] private CategoryGridItem itemPrefab;

    // Optional: force an order of types; else whatever the catalog gives
    [SerializeField] private List<string> typeOrder; // or an enum if you prefer

    private Dictionary<string, List<CategoryEntry>> _byType;

    [System.Serializable]
    public class CategoryEntry
    {
        public string Id;
        public string DisplayName;
        public Sprite Icon;
        public string Type;
        public float Percent; // if you want to show progress
    }

    private void Awake()
    {
        BuildIndexFromCatalog();
    }

    public void Rebuild()
    {
        // Clear
        for (int i = contentVertical.childCount - 1; i >= 0; i--)
            Destroy(contentVertical.GetChild(i).gameObject);

        var types = (typeOrder != null && typeOrder.Count > 0)
            ? typeOrder.Where(t => _byType.ContainsKey(t))
            : _byType.Keys;

        foreach (var t in types)
        {
            var section = Instantiate(sectionPrefab, contentVertical);
            section.SetHeader(t);

            var items = _byType[t]
                .OrderBy(c => c.DisplayName)
                .Select(c => (c.Id, c.DisplayName, c.Icon));

            section.Populate(items, itemPrefab);
        }
    }

    private void BuildIndexFromCatalog()
    {
        _byType = new Dictionary<string, List<CategoryEntry>>();

        // TODO: Replace this with YOUR real catalog accessors.
        // The idea: read all categories from your catalog (same data that feeds the carousel),
        // fill _byType[type] = list of CategoryEntry.

        // Example (pseudo):
        // foreach (var type in catalog.Types)
        //   foreach (var cat in type.Categories)
        //      Add(cat.Id, cat.DisplayName, cat.Icon, type.Name);

        // After building:
        Rebuild();
    }
}
