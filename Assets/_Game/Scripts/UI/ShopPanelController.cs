using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Handles the in-game coin shop popup:
/// - Opens with fade/slide animation
/// - Buys coin packs (stubbed; calls SaveSystem.AddCoins)
/// - Closes with fade/slide
/// </summary>
public class ShopPanelController : MonoBehaviour
{
    public static ShopPanelController Instance;

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;     // required for fade
    [SerializeField] private RectTransform window;        // the inner window to slide (not the whole screen BG)
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
    [SerializeField] private EconomyConfig economy;       // drag your EconomyConfig asset here

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.25f;  // seconds
    [SerializeField] private float slideDistance = 120f;  // pixels to slide up from
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);

    private Coroutine animRoutine;

    private void Awake()
    {
        Instance = this;

        // Hook buttons
        if (btnClose)      { btnClose.onClick.RemoveAllListeners();      btnClose.onClick.AddListener(Close); }
        if (btnSmallPack)  { btnSmallPack.onClick.RemoveAllListeners();  btnSmallPack.onClick.AddListener(BuySmallPack); }
        if (btnMediumPack) { btnMediumPack.onClick.RemoveAllListeners(); btnMediumPack.onClick.AddListener(BuyMediumPack); }
        if (btnLargePack)  { btnLargePack.onClick.RemoveAllListeners();  btnLargePack.onClick.AddListener(BuyLargePack); }

        // Ensure default hidden state is clean
        if (canvasGroup) canvasGroup.alpha = 0f;
        // Do NOT auto-deactivate here; let the scene decide. We handle activation in Open().
        gameObject.SetActive(gameObject.activeSelf); 
    }

    // -------- Static helper to show from anywhere --------
    public static void Show()
    {
        if (Instance == null)
        {
            // Try to find an instance in the scene (including inactive)
            Instance = Object.FindFirstObjectByType<ShopPanelController>(FindObjectsInactive.Include);
            if (Instance == null)
            {
                Debug.LogWarning("[ShopPanel] No instance found in scene.");
                return;
            }
        }
        Instance.Open();
    }

    // -------- Public API --------
    public void Open()
    {
        RefreshLabels();

        // ✅ Activate before starting any coroutine (fixes the warning)
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateOpen());
    }

    public void Close()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateClose());
    }

    // -------- Purchases (stubbed) --------
    private void BuySmallPack()  => PurchaseCoins(economy ? economy.packSmallCoins  : 100);
    private void BuyMediumPack() => PurchaseCoins(economy ? economy.packMediumCoins : 300);
    private void BuyLargePack()  => PurchaseCoins(economy ? economy.packLargeCoins  : 1000);

    private void PurchaseCoins(int amount)
    {
        SaveSystem.AddCoins(amount);
        Debug.Log($"[ShopPanel] Player bought {amount} coins.");
        Close();
    }

    // -------- Internals --------
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

        float t = 0f;
        float dur = Mathf.Max(0.01f, fadeDuration);

        // Prepare start state
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

        float t = 0f;
        float dur = Mathf.Max(0.01f, fadeDuration);

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
