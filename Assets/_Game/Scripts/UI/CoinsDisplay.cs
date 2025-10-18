using UnityEngine;
using TMPro;
using System.Collections;

[DisallowMultipleComponent]
public class CoinsDisplay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("TMP text to display the coin amount. If left empty, the first TMP_Text in children will be used.")]
    [SerializeField] private TMP_Text coinsText;

    [Header("Format")]
    [Tooltip("Optional prefix/suffix, e.g., 'Coins: {0}' or '{0} 🪙'. Use {0} as the number placeholder.")]
    [SerializeField] private string format = "{0}";

    [Header("Animation")]
    [Tooltip("Animate the label briefly when the value changes.")]
    [SerializeField] private bool animateOnChange = true;
    [SerializeField] private float punchScale = 1.12f;
    [SerializeField] private float punchInSeconds = 0.08f;
    [SerializeField] private float punchOutSeconds = 0.12f;

    private Coroutine animCo;
    private Vector3 baseScale;

    private void Awake()
    {
        // Ensure save data exists if a scene is run directly.
        if (SaveSystem.Data == null)
            SaveSystem.Load();

        if (!coinsText)
            coinsText = GetComponentInChildren<TMP_Text>(true);

        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        SaveSystem.OnCoinsChanged += HandleCoinsChanged;
        // Initialize immediately
        HandleCoinsChanged(SaveSystem.Data.coins);
    }

    private void OnDisable()
    {
        SaveSystem.OnCoinsChanged -= HandleCoinsChanged;
    }

    private void HandleCoinsChanged(int newCoins)
    {
        if (!coinsText) return;

        // Update text with optional formatting
        coinsText.text = string.IsNullOrEmpty(format)
            ? newCoins.ToString()
            : string.Format(format, newCoins);

        if (animateOnChange)
        {
            if (animCo != null) StopCoroutine(animCo);
            animCo = StartCoroutine(Punch());
        }
    }

    private IEnumerator Punch()
    {
        // Quick scale up
        float t = 0f;
        while (t < punchInSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / punchInSeconds);
            transform.localScale = Vector3.Lerp(baseScale, baseScale * punchScale, k);
            yield return null;
        }

        // Scale back down
        t = 0f;
        while (t < punchOutSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / punchOutSeconds);
            transform.localScale = Vector3.Lerp(baseScale * punchScale, baseScale, k);
            yield return null;
        }

        transform.localScale = baseScale;
        animCo = null;
    }

    // Optional manual refresh if you ever need it
    public void ForceRefresh()
    {
        HandleCoinsChanged(SaveSystem.Data != null ? SaveSystem.Data.coins : 0);
    }
}
