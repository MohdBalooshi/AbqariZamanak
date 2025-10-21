using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinsGatePopup : MonoBehaviour
{
    public static CoinsGatePopup Instance;

    [Header("UI References")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button btnWatchAd;
    [SerializeField] private Button btnOpenShop;
    [SerializeField] private Button btnCancel;

    [Header("Economy")]
    [SerializeField] private EconomyConfig economy;

    private int _needed;

    private void Awake()
    {
        Instance = this;
        if (btnWatchAd)  { btnWatchAd.onClick.RemoveAllListeners();  btnWatchAd.onClick.AddListener(OnWatchAd); }
        if (btnOpenShop) { btnOpenShop.onClick.RemoveAllListeners(); btnOpenShop.onClick.AddListener(OnOpenShop); }
        if (btnCancel)   { btnCancel.onClick.RemoveAllListeners();   btnCancel.onClick.AddListener(Close); }

        // Start hidden
        gameObject.SetActive(false);
    }

    // ---------- Public API ----------
    public static void Show(int needed)
    {
        // Ensure an instance exists in the active scene
        if (Instance == null)
        {
            // Try find an inactive one first (new Unity API)
            Instance = Object.FindFirstObjectByType<CoinsGatePopup>(FindObjectsInactive.Include);

            // If still null, try spawn from Resources
            if (Instance == null)
            {
                var spawned = TrySpawnFromResources();
                if (spawned != null) Instance = spawned;
            }

            if (Instance == null)
            {
                Debug.LogWarning("[CoinsGatePopup] Instance missing and no prefab at Resources/CoinsGatePopup. Cannot show popup.");
                return;
            }
        }

        Instance._needed = needed;
        Instance.Refresh();
        Instance.gameObject.SetActive(true);
    }

    // ---------- Internals ----------
    private static CoinsGatePopup TrySpawnFromResources()
    {
        // Expect a prefab at Assets/Resources/CoinsGatePopup.prefab
        var prefab = Resources.Load<CoinsGatePopup>("CoinsGatePopup");
        if (!prefab)
        {
            return null;
        }

        // Ensure there is a Canvas to parent under
        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (!canvas)
        {
            var goCanvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = goCanvas.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas = c;
        }

        var instance = Object.Instantiate(prefab, canvas.transform);
        instance.gameObject.SetActive(false); // will be set active by Show()
        return instance;
    }

    private void Refresh()
    {
        int have = SaveSystem.GetCoins();
        int miss = Mathf.Max(0, _needed - have);
        if (messageText)
            messageText.text = $"Not enough coins!\nNeed {_needed}, you have {have} (missing {miss}).";
    }

    private void OnWatchAd()
    {
        int reward = economy ? economy.adCoinsReward : 10;
        AdsManager.ShowRewarded(success =>
        {
            if (success) SaveSystem.AddCoins(reward);
            Close();
        });
    }

    private void OnOpenShop()
    {
        var shop = Object.FindFirstObjectByType<ShopPanelController>(FindObjectsInactive.Include);
        if (shop) shop.Open();
        Close();
    }

    private void Close() => gameObject.SetActive(false);
}
