using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class TypeSectionAutoHeight : MonoBehaviour
{
    [SerializeField] private RectTransform header;          // assign: Header
    [SerializeField] private GridLayoutGroup grid;          // assign: Grid
    [SerializeField] private LayoutElement layoutElement;   // assign: LayoutElement on this TypeSection
    [SerializeField] private int columns = 3;
    [SerializeField] private float headerToGridSpacing = 16f;

    private void OnEnable()                        => Recalc();
    private void OnTransformChildrenChanged()      => Recalc();
    private void OnRectTransformDimensionsChange() => Recalc();

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying) Recalc();
    }
#endif

    private void Recalc()
    {
        if (!header || !grid || !layoutElement) return;

        // How many active tiles are inside the grid?
        int items = 0;
        for (int i = 0; i < grid.transform.childCount; i++)
            if (grid.transform.GetChild(i).gameObject.activeInHierarchy) items++;

        int rows = Mathf.Max(1, Mathf.CeilToInt(items / (float)columns));

        // Header height (use its LayoutElement if set)
        float headerH = header.sizeDelta.y;
        var headerLE = header.GetComponent<LayoutElement>();
        if (headerLE && headerLE.preferredHeight > 0) headerH = headerLE.preferredHeight;

        // Grid total height = padding + rows*cell + gaps
        var pad = grid.padding;
        float gridH = pad.top + pad.bottom +
                      rows * grid.cellSize.y +
                      (rows - 1) * grid.spacing.y;

        // Section’s preferred height that the parent (Content_Vertical) will sum
        layoutElement.preferredHeight = headerH + headerToGridSpacing + gridH;
    }
}
