using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Displays the current coin balance and auto-updates whenever it changes.
/// Works in any scene (Main Menu, Category Select, Level Select, Quiz, etc.)
/// Adds a small pop animation when the value changes.
/// </summary>
public class CoinHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text coinsText;

    [Header("Pop Animation")]
    [SerializeField] private bool popOnChange = true;
    [SerializeField] private float popScale = 1.12f;
    [SerializeField] private float popTime = 0.12f;

    private Coroutine popRoutine;

    private void Awake()
    {
        if (SaveSystem.Data == null)
            SaveSystem.Load();
    }

    private void OnEnable()
    {
        SaveSystem.OnCoinsChanged += HandleCoinsChanged;
        Refresh();
    }

    private void OnDisable()
    {
        SaveSystem.OnCoinsChanged -= HandleCoinsChanged;
    }

    private void HandleCoinsChanged(int _)
    {
        Refresh();

        if (popOnChange && coinsText != null)
        {
            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(Pop());
        }
    }

    private void Refresh()
    {
        if (!coinsText) coinsText = GetComponent<TMP_Text>();
        if (!coinsText) return;

        int c = SaveSystem.GetCoins();
        coinsText.text = Abbrev(c);
    }

    private static string Abbrev(int n)
    {
        if (n >= 1_000_000) return (n / 1_000_000f).ToString("0.#") + "M";
        if (n >= 1_000)     return (n / 1_000f).ToString("0.#") + "K";
        return n.ToString();
    }

    private IEnumerator Pop()
    {
        var t = coinsText.transform;
        Vector3 baseScale = Vector3.one;
        Vector3 bigScale = Vector3.one * popScale;

        float t0 = 0f;
        // Scale up
        while (t0 < popTime)
        {
            t0 += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t0 / popTime);
            t.localScale = Vector3.Lerp(baseScale, bigScale, k);
            yield return null;
        }

        // Scale back down
        t0 = 0f;
        while (t0 < popTime)
        {
            t0 += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t0 / popTime);
            t.localScale = Vector3.Lerp(bigScale, baseScale, k);
            yield return null;
        }

        t.localScale = baseScale;
        popRoutine = null;
    }
}
