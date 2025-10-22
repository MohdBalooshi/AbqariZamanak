using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// In-game coin shop popup:
/// - Auto-spawns from Resources if no instance exists
/// - Opens with fade/slide animation
/// - Buys coin packs (adds coins via SaveSystem)
/// </summary>
public class ShopPanelController : MonoBehaviour
{
    public static ShopPanelController Instance;

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;     // required for fade
    [SerializeField] private RectTransform window;        // inner card to slide
    [SerializeField] private TMP_Text titleText;

    [Header("Buttons")]
    [SerializeField] private Button btnSmallPack;
    [SerializeField] private Button btnMediumPack;
    [SerializeField] private Button btnLargePack;
    [SerializeField] private Button btnClose;

    [Header("Labels (optional)")]
    [SerializeField] private TMP_Text smallLabel;         // e.g. "+100 coins"
    [SerializeField] private TMP_Text mediumLabel;        // e.g. "+300 coins"
    [SerializeField] private TMP_Text largeLabel;         // e.g. "+1000 coins"

    [Header("Economy")]
    [SerializeField] private EconomyConfig economy;       // drag EconomyConfig asset (if prefab, wire it there)

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float slideDistance = 120f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);

    private Coroutine animRoutine;

    private void Awake()
    {
        Instance = this;

        if (btnClose)      { btnClose.onClick.RemoveAllListeners();      btnClose.onClick.AddListener(Close); }
        if (btnSmallPack)  { btnSmallPack.onClick.RemoveAllListeners();  btnSmallPack.onClick.AddListener(BuySmallPack); }
        if (btnMediumPack) { btnMediumPack.onClick.RemoveAllListeners(); btnMediumPack.onClick.AddListener(BuyMediumPack); }
        if (btnLargePack)  { btnLargePack.onClick.RemoveAllListeners();  btnLargePack.onClick.AddListener(BuyLargePack); }

        if (canvasGroup) canvasGroup.alpha = 0f; // start hidden
        // Keep active state as-is; Open() will ensure activation.
    }

    // ---------- Static API ----------
    public static void Show()
    {
        if (Instance == null)
        {
            // try find (including inactive)
            Instance = Object.FindFirstObjectByType<ShopPanelController>(FindObjectsInactive.Include);

            // auto-spawn from Resources if still missing
            if (Instance == null)
            {
                Instance = TrySpawnFromResources();
                if (Instance == null)
                {
                    Debug.LogWarning("[ShopPanel] No instance and no prefab at Resources/ShopPanel. Cannot open shop.");
                    return;
                }
            }
        }

        Instance.Open();
    }

    private static ShopPanelController TrySpawnFromResources()
    {
        // Expect: Assets/Resources/ShopPanel.prefab (root has this script)
        var prefab = Resources.Load<ShopPanelController>("ShopPanel");
        if (!prefab) return null;

        // Find a Canvas to parent under, or create one
        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (!canvas)
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas = c;
        }

        var inst = Object.Instantiate(prefab, canvas.transform);
        inst.gameObject.SetActive(true); // make sure it's active so coroutines can run
        return inst;
    }

    // ---------- Instance API ----------
    public void Open()
    {
        RefreshLabels();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);         // ensure active before coroutine

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateOpen());
    }

    public void Close()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateClose());
    }

    // ---------- Purchases (stub) ----------
    private void BuySmallPack()  => PurchaseCoins(economy ? economy.packSmallCoins  : 100);
    private void BuyMediumPack() => PurchaseCoins(economy ? economy.packMediumCoins : 300);
    private void BuyLargePack()  => PurchaseCoins(economy ? economy.packLargeCoins  : 1000);

    private void PurchaseCoins(int amount)
    {
        SaveSystem.AddCoins(amount);
        Debug.Log($"[ShopPanel] Player bought {amount} coins.");
        Close();
    }

    // ---------- Internals ----------
    private void RefreshLabels()
    {
        if (titleText) titleText.text = "Coin Shop";
        if (!economy) return;
        if (smallLabel)  smallLabel.text  = $"+{economy.packSmallCoins} coins";
        if (mediumLabel) mediumLabel.text = $"+{economy.packMediumCoins} coins";
        if (largeLabel)  largeLabel.text  = $"+{economy.packLargeCoins} coins";
    }

    private IEnumerator AnimateOpen()
    {
        if (!canvasGroup) yield break;

        float t = 0f, dur = Mathf.Max(0.01f, fadeDuration);
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable   = true;
        canvasGroup.alpha = 0f;

        Vector2 startPos = window ? window.anchoredPosition : Vector2.zero;
        Vector2 fromPos  = startPos + Vector2.down * slideDistance;
        if (window) window.anchoredPosition = fromPos;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = ease.Evaluate(Mathf.Clamp01(t / dur));
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, k);
            if (window) window.anchoredPosition = Vector2.Lerp(fromPos, startPos, k);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        if (window) window.anchoredPosition = startPos;
        animRoutine = null;
    }

    private IEnumerator AnimateClose()
    {
        if (!canvasGroup) { gameObject.SetActive(false); yield break; }

        float t = 0f, dur = Mathf.Max(0.01f, fadeDuration);
        Vector2 startPos = window ? window.anchoredPosition : Vector2.zero;
        Vector2 toPos    = startPos + Vector2.down * slideDistance;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = ease.Evaluate(Mathf.Clamp01(t / dur));
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, k);
            if (window) window.anchoredPosition = Vector2.Lerp(startPos, toPos, k);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        if (window) window.anchoredPosition = toPos;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable   = false;

        gameObject.SetActive(false);
        animRoutine = null;
    }
}
