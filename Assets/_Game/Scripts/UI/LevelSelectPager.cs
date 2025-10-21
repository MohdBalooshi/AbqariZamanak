using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSelectPager : MonoBehaviour
{
    [Header("Category")]
    [SerializeField] private string categoryId;    // set from your flow (or read QuizContext if you prefer)

    [Header("Pages (1..5)")]
    [SerializeField] private GameObject page1; // levels 1-20
    [SerializeField] private GameObject page2; // 21-40
    [SerializeField] private GameObject page3; // 41-60
    [SerializeField] private GameObject page4; // 61-80
    [SerializeField] private GameObject page5; // 81-100

    [Header("Nav Buttons")]
    [SerializeField] private Button btnPrev;
    [SerializeField] private Button btnNext;
    [SerializeField] private TMP_Text pageLabel;

    private int currentPage = 1;

    private void Start()
    {
        if (string.IsNullOrEmpty(categoryId) && !string.IsNullOrEmpty(QuizContext.SelectedCategoryId))
            categoryId = QuizContext.SelectedCategoryId;

        if (btnPrev) { btnPrev.onClick.RemoveAllListeners(); btnPrev.onClick.AddListener(PrevPage); }
        if (btnNext) { btnNext.onClick.RemoveAllListeners(); btnNext.onClick.AddListener(NextPage); }

        ShowPage(1);
        UpdateNavState();
    }

    private void ShowPage(int page)
    {
        currentPage = Mathf.Clamp(page, 1, 5);
        if (pageLabel) pageLabel.text = $"Page {currentPage}/5";

        if (page1) page1.SetActive(currentPage == 1);
        if (page2) page2.SetActive(currentPage == 2);
        if (page3) page3.SetActive(currentPage == 3);
        if (page4) page4.SetActive(currentPage == 4);
        if (page5) page5.SetActive(currentPage == 5);

        UpdateNavState();
    }

    private void PrevPage()
    {
        ShowPage(currentPage - 1);
    }

    private void NextPage()
    {
        // Gate: only allow moving to next page if previous page range is 100% complete
        if (!CanOpenPage(currentPage + 1))
        {
            Debug.Log("[LevelSelectPager] Next page locked — complete the previous 20 levels first.");
            return;
        }
        ShowPage(currentPage + 1);
    }

    private bool CanOpenPage(int page)
    {
        if (page <= 1) return true; // page 1 is always open
        if (string.IsNullOrEmpty(categoryId)) return false;

        // required completed range:
        // to open 2: 1-20 complete
        // to open 3: 1-40 complete
        // to open 4: 1-60 complete
        // to open 5: 1-80 complete
        int end = (page - 1) * 20;
        return SaveSystem.AreLevelsCompleteInRange(categoryId, 1, end);
    }

    private void UpdateNavState()
    {
        if (btnPrev) btnPrev.interactable = (currentPage > 1);
        if (btnNext) btnNext.interactable = (currentPage < 5) && CanOpenPage(currentPage + 1);
    }
}
