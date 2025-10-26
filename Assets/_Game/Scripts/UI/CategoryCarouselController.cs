using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class CategoryCarouselController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Data")]
    [SerializeField] private CategoryTypesCatalog catalog;

    [Header("Scenes")]
    [SerializeField] private string levelSelectSceneName = "LevelSelect";
    [SerializeField] private string mainMenuSceneName   = "MainMenu";

    [Header("Top (Type Switch)")]
    [SerializeField] private TMP_Text typeTitleText;
    [SerializeField] private Button btnTypePrev;
    [SerializeField] private Button btnTypeNext;

    [Header("Category Card / Swipe Area")]
    [SerializeField] private RectTransform swipeArea;     // transparent Image to catch swipes
    [SerializeField] private Button categoryTapButton;    // clicking the card can also play
    [SerializeField] private Image categoryArt;           // optional art per category
    [SerializeField] private TMP_Text catTitleBig;        // optional big name on the card

    [Header("Info Panel")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelsText;
    [SerializeField] private TMP_Text reachedText;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private Image percentFill;           // Image Type = Filled (Horizontal, Origin Left)

    [Header("Bottom Controls")]
    [SerializeField] private Button btnCatPrev;
    [SerializeField] private Button btnCatNext;
    [SerializeField] private Button btnPlay;
    [SerializeField] private Button btnBackToMenu;

    [Header("Swipe Settings")]
    [SerializeField] private float minSwipeDistance = 80f; // pixels

    // Optional small slide animation (hook the CategoryPanel if you want the tween)
    [SerializeField] private RectTransform categoryPanel;
    [SerializeField] private float slideDistance = 120f;
    [SerializeField] private float slideDuration = 0.15f;
    private Coroutine slideCo;

    // Internal state
    private int typeIndex = 0;
    private int catIndex  = 0;

    private IReadOnlyDictionary<string, CategoryBank> banks;
    private Vector2 pointerDownPos;
    private bool pointerTracking = false;

    private void Awake()
    {
        if (SaveSystem.Data == null) SaveSystem.Load();
    }

    private void Start()
    {
        // Load question banks
        if (QuestionDB.Banks == null || QuestionDB.Banks.Count == 0)
            QuestionDB.LoadAllFromResources();
        banks = QuestionDB.Banks;

        // Wire buttons
        if (btnTypePrev) { btnTypePrev.onClick.RemoveAllListeners(); btnTypePrev.onClick.AddListener(PrevType); }
        if (btnTypeNext) { btnTypeNext.onClick.RemoveAllListeners(); btnTypeNext.onClick.AddListener(NextType); }
        if (btnCatPrev)  { btnCatPrev.onClick.RemoveAllListeners();  btnCatPrev.onClick.AddListener(PrevCategory); }
        if (btnCatNext)  { btnCatNext.onClick.RemoveAllListeners();  btnCatNext.onClick.AddListener(NextCategory); }
        if (btnPlay)     { btnPlay.onClick.RemoveAllListeners();     btnPlay.onClick.AddListener(PlaySelected); }
        if (btnBackToMenu){ btnBackToMenu.onClick.RemoveAllListeners(); btnBackToMenu.onClick.AddListener(()=> SceneManager.LoadScene(mainMenuSceneName)); }
        if (categoryTapButton){ categoryTapButton.onClick.RemoveAllListeners(); categoryTapButton.onClick.AddListener(PlaySelected); }

        // Clamp to a valid type that has categories
        SnapToFirstValidType();
        RefreshTypeUI();
        RefreshCategoryUI();
    }

    // ---------- Swipe ----------
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsPointerInSwipeArea(eventData)) return;
        pointerTracking = true;
        pointerDownPos = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!pointerTracking) return;
        pointerTracking = false;

        float dx = eventData.position.x - pointerDownPos.x;
        if (Mathf.Abs(dx) >= minSwipeDistance)
        {
            if (dx < 0) NextCategory(); // swipe left -> next
            else        PrevCategory(); // swipe right -> prev
        }
    }

    private bool IsPointerInSwipeArea(PointerEventData e)
    {
        if (!swipeArea) return false;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            swipeArea, e.position, e.pressEventCamera, out var local);
        return new Rect(swipeArea.rect.xMin, swipeArea.rect.yMin, swipeArea.rect.width, swipeArea.rect.height)
            .Contains(local);
    }

    // ---------- Type navigation ----------
    private void SnapToFirstValidType()
    {
        if (catalog == null || catalog.types == null || catalog.types.Count == 0)
        {
            Debug.LogError("[CategoryCarousel] Catalog is empty.");
            return;
        }
        typeIndex = Mathf.Clamp(typeIndex, 0, catalog.types.Count - 1);

        // ensure current type has categories
        int tries = catalog.types.Count;
        while (tries-- > 0)
        {
            var list = CurrentCategoryIds();
            if (list != null && list.Count > 0) break;
            typeIndex = (typeIndex + 1) % catalog.types.Count;
        }
        catIndex = 0;
    }

    private void PrevType()
    {
        typeIndex = (typeIndex - 1 + catalog.types.Count) % catalog.types.Count;
        EnsureTypeHasCategories();
        catIndex = 0;
        RefreshTypeUI();
        RefreshCategoryUI();
    }

    private void NextType()
    {
        typeIndex = (typeIndex + 1) % catalog.types.Count;
        EnsureTypeHasCategories();
        catIndex = 0;
        RefreshTypeUI();
        RefreshCategoryUI();
    }

    private void EnsureTypeHasCategories()
    {
        int safety = catalog.types.Count;
        while (safety-- > 0)
        {
            var list = CurrentCategoryIds();
            if (list != null && list.Count > 0) return;
            typeIndex = (typeIndex + 1) % catalog.types.Count;
        }
    }

    private void RefreshTypeUI()
    {
        var ct = catalog.types[typeIndex];
        if (typeTitleText) typeTitleText.text = string.IsNullOrEmpty(ct.displayName) ? ct.typeId : ct.displayName;
    }

    // ---------- Category navigation ----------
    private void PrevCategory()
    {
        var list = CurrentCategoryIds();
        if (list == null || list.Count == 0) return;
        catIndex = (catIndex - 1 + list.Count) % list.Count;
        RefreshCategoryUI();
        AnimateSlide(-1);
    }

    private void NextCategory()
    {
        var list = CurrentCategoryIds();
        if (list == null || list.Count == 0) return;
        catIndex = (catIndex + 1) % list.Count;
        RefreshCategoryUI();
        AnimateSlide(+1);
    }

    private void RefreshCategoryUI()
    {
        var list = CurrentCategoryIds();
        if (list == null || list.Count == 0)
        {
            if (nameText) nameText.text = "No categories";
            if (levelsText) levelsText.text = "Max Levels: 0";
            if (reachedText) reachedText.text = "Reached: 0";
            if (percentText) percentText.text = "0%";
            if (percentFill) percentFill.fillAmount = 0f;
            if (catTitleBig) catTitleBig.text = "";
            return;
        }

        string catId = list[Mathf.Clamp(catIndex, 0, list.Count - 1)];

        // Bank lookup (FIX for CS0165)
        CategoryBank bank = null;
        if (banks != null)
            banks.TryGetValue(catId, out bank);

        // Title(s)
        string displayName = bank != null ? bank.categoryName : catId;
        if (nameText)     nameText.text = displayName;
        if (catTitleBig)  catTitleBig.text = displayName;

        // Optional art: keep disabled unless you assign sprites via your own map
        if (categoryArt) categoryArt.enabled = (categoryArt.sprite != null);

        // Stats
        int totalLevels = GetTotalLevels(bank);
        int reached     = HighestReachableLevel(catId, totalLevels); // uses IsLevelComplete
        int totalQs     = TotalQuestionCount(bank);
        float percent   = (totalQs > 0) ? SaveSystem.GetPercent(catId, totalQs) : 0f;

        if (levelsText)  levelsText.text  = $"Max Levels: {totalLevels}";
        if (reachedText) reachedText.text = $"Reached: {Mathf.Clamp(reached, 0, totalLevels)}";
        if (percentText) percentText.text = $"{Mathf.RoundToInt(percent)}%";
        if (percentFill)
        {
            percentFill.type = Image.Type.Filled;
            percentFill.fillMethod = Image.FillMethod.Horizontal;
            percentFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            percentFill.fillAmount = Mathf.Clamp01(percent / 100f);
        }
    }

    // ---------- Actions ----------
    private void PlaySelected()
    {
        var list = CurrentCategoryIds();
        if (list == null || list.Count == 0) return;

        string catId = list[Mathf.Clamp(catIndex, 0, list.Count - 1)];
        QuizContext.SelectedCategoryId = catId;

        if (!Application.CanStreamedLevelBeLoaded(levelSelectSceneName))
        {
            Debug.LogError($"[CategoryCarousel] Scene '{levelSelectSceneName}' not in Build Settings.");
            return;
        }
        SceneManager.LoadScene(levelSelectSceneName);
    }

    // ---------- Helpers ----------
    private List<string> CurrentCategoryIds()
    {
        if (catalog == null || catalog.types == null || catalog.types.Count == 0) return null;
        var ct = catalog.types[Mathf.Clamp(typeIndex, 0, catalog.types.Count - 1)];
        return (ct != null && ct.categoryIds != null) ? ct.categoryIds : null;
    }

    private static int GetTotalLevels(CategoryBank b)
    {
        if (b == null) return 0;
        if (b.levels != null && b.levels.Count > 0) return b.levels.Count;
        return (b.questions != null && b.questions.Count > 0) ? 1 : 0;
    }

    // FIX: compute “reachable” using IsLevelComplete (no IsLevelUnlocked needed)
    private static int HighestReachableLevel(string categoryId, int totalLevels)
    {
        if (totalLevels <= 0) return 0;

        // Level 1 is always reachable by default
        int highestReachable = 1;

        // For each completed level i, the next level (i+1) becomes reachable.
        for (int i = 1; i <= totalLevels; i++)
        {
            if (SaveSystem.IsLevelComplete(categoryId, i))
                highestReachable = Mathf.Min(i + 1, totalLevels);
            else
                break; // stop at first incomplete
        }

        return Mathf.Clamp(highestReachable, 1, totalLevels);
    }

    private static int TotalQuestionCount(CategoryBank b)
    {
        if (b == null) return 0;
        if (b.levels != null && b.levels.Count > 0)
            return b.levels.Sum(l => l.questions != null ? l.questions.Count : 0);
        return b.questions != null ? b.questions.Count : 0;
    }

    // --- Optional slide tween for polish ---
    private void AnimateSlide(int dir)
    {
        if (!categoryPanel) return;
        if (slideCo != null) StopCoroutine(slideCo);
        slideCo = StartCoroutine(SlideRoutine(dir));
    }

    private System.Collections.IEnumerator SlideRoutine(int dir)
    {
        Vector2 start = categoryPanel.anchoredPosition;
        Vector2 mid   = start + new Vector2(-dir * slideDistance, 0f);
        Vector2 end   = start;

        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / slideDuration;
            // ease out
            k = 1f - Mathf.Pow(1f - k, 3f);
            categoryPanel.anchoredPosition = Vector2.Lerp(start, mid, k);
            yield return null;
        }

        categoryPanel.anchoredPosition = end;
        slideCo = null;
    }
    // Add inside CategoryCarouselController (exact names used by SwipeRelay)
public void BeginSwipe(Vector2 screenPos, Camera cam)
{
    if (!swipeArea) return;
    // store pointer down only if inside swipeArea rect
    RectTransformUtility.ScreenPointToLocalPointInRectangle(swipeArea, screenPos, cam, out var local);
    var r = new Rect(swipeArea.rect.xMin, swipeArea.rect.yMin, swipeArea.rect.width, swipeArea.rect.height);
    if (!r.Contains(local)) return;

    pointerTracking = true;
    pointerDownPos = screenPos;
}

public void EndSwipe(Vector2 screenPos, Camera cam)
{
    if (!pointerTracking) return;
    pointerTracking = false;

    float dx = screenPos.x - pointerDownPos.x;
    if (Mathf.Abs(dx) >= minSwipeDistance)
    {
        if (dx < 0) NextCategory();  // swipe left -> next
        else        PrevCategory();  // swipe right -> prev
    }
}

}
