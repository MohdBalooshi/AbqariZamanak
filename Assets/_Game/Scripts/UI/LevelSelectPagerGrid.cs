using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSelectPagerGrid : MonoBehaviour
{
    [Header("Category")]
    [SerializeField] private string categoryId; // leave empty to auto-read QuizContext.SelectedCategoryId

    [Header("Grid Root (parent of ALL level buttons)")]
    [SerializeField] private Transform gridRoot;   // your content that has all Level buttons as children

    [Header("UI")]
    [SerializeField] private Button btnPrev;
    [SerializeField] private Button btnNext;
    [SerializeField] private TMP_Text pageLabel;   // e.g., "Page 1/5"

    [Header("Paging")]
    [SerializeField] private int levelsPerPage = 20;

    private readonly List<GameObject> _items = new();
    private int _pageCount = 1;
    private int _currentPage = 1;

    private void Awake()
    {
        if (string.IsNullOrEmpty(categoryId) && !string.IsNullOrEmpty(QuizContext.SelectedCategoryId))
            categoryId = QuizContext.SelectedCategoryId;
    }

    private void Start()
    {
        if (!gridRoot)
        {
            Debug.LogError("[LevelSelectPagerGrid] gridRoot is not assigned.");
            return;
        }

        // Collect children in hierarchy order (siblingIndex)
        _items.Clear();
        for (int i = 0; i < gridRoot.childCount; i++)
        {
            var child = gridRoot.GetChild(i).gameObject;
            _items.Add(child);
        }

        // Compute page count (ceil)
        _pageCount = Mathf.Max(1, Mathf.CeilToInt(_items.Count / (float)levelsPerPage));

        // Wire buttons
        if (btnPrev) { btnPrev.onClick.RemoveAllListeners(); btnPrev.onClick.AddListener(PrevPage); }
        if (btnNext) { btnNext.onClick.RemoveAllListeners(); btnNext.onClick.AddListener(NextPage); }

        // Start at page 1
        ShowPage(1);
    }

    private void PrevPage()
    {
        ShowPage(_currentPage - 1);
    }

    private void NextPage()
    {
        int target = _currentPage + 1;
        if (!CanOpenPage(target))
        {
            Debug.Log("[LevelSelectPagerGrid] Next page locked — complete the previous 20 levels first.");
            return;
        }
        ShowPage(target);
    }

    private void ShowPage(int page)
    {
        if (_items.Count == 0) return;

        _currentPage = Mathf.Clamp(page, 1, _pageCount);

        // Hide all
        for (int i = 0; i < _items.Count; i++)
            _items[i].SetActive(false);

        // Compute visible range [start..endExclusive)
        int startIndex = (_currentPage - 1) * levelsPerPage;     // 0-based
        int endExclusive = Mathf.Min(startIndex + levelsPerPage, _items.Count);

        // Show only current page’s items
        for (int i = startIndex; i < endExclusive; i++)
            _items[i].SetActive(true);

        // Update UI
        if (pageLabel) pageLabel.text = $"Page {_currentPage}/{_pageCount}";
        UpdateNavState();
    }

    private void UpdateNavState()
    {
        if (btnPrev) btnPrev.interactable = (_currentPage > 1);
        if (btnNext) btnNext.interactable = (_currentPage < _pageCount) && CanOpenPage(_currentPage + 1);
    }

    /// <summary>
    /// Gate pages:
    /// Page 2 requires levels 1..20 complete.
    /// Page 3 requires 1..40, etc.
    /// </summary>
    private bool CanOpenPage(int page)
    {
        if (page <= 1) return true;             // page 1 always open
        if (page > _pageCount) return false;
        if (string.IsNullOrEmpty(categoryId)) return false;

        int endLevelRequired = (page - 1) * levelsPerPage; // e.g., page 2 => 20, page 3 => 40
        return SaveSystem.AreLevelsCompleteInRange(categoryId, 1, endLevelRequired);
    }
}
