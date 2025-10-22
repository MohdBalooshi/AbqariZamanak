using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class QuizTimed : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] private TMP_Text categoryTitle;
    [SerializeField] private TMP_Text questionCounterText;
    [SerializeField] private TMP_Text coinsText;

    [Header("Question UI")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button btnA;
    [SerializeField] private Button btnB;
    [SerializeField] private Button btnC;
    [SerializeField] private Button btnD;
    [SerializeField] private TMP_Text labelA;
    [SerializeField] private TMP_Text labelB;
    [SerializeField] private TMP_Text labelC;
    [SerializeField] private TMP_Text labelD;

    [Header("Per-Question Timer")]
    [SerializeField] private Image timerFill;        // Image (Type = Filled, Method = Horizontal, Origin = Left)
    [SerializeField] private TMP_Text timerText;     // optional seconds label
    [SerializeField] private float secondsPerQuestion = 15f;

    [Header("Round Config")]
    [SerializeField] private int questionsPerRound = 10;
    [SerializeField] private int unlockThreshold   = 8;

    [Header("Results UI")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TMP_Text resultsText;
    [SerializeField] private Button btnToMenu;

    [Header("Failure UI")]
    [SerializeField] private GameObject failPanel;
    [SerializeField] private Button btnAdRetry;
    [SerializeField] private Button btnQuitFail;     // (optional) quit from fail panel -> LevelSelect

    [Header("Quit Flow (in-quiz)")]
    [SerializeField] private Button btnQuitTop;      // top-right quit button
    [SerializeField] private GameObject quitConfirmPanel; // inactive by default
    [SerializeField] private CanvasGroup quitConfirmCg;   // optional fade
    [SerializeField] private Button btnQuitConfirm;       // confirm quit -> LevelSelect
    [SerializeField] private Button btnQuitCancel;        // close panel
    [SerializeField] private float quitFade = 0.15f;

    [Header("Feedback")]
    [SerializeField] private ParticleSystem confetti;
    [SerializeField] private GameObject toastPanel;
    [SerializeField] private TMP_Text toastText;
    [SerializeField] private float toastSeconds = 1.4f;

    // Internal state
    private CategoryBank bank;
    private LevelBank level;
    private List<QuestionEntry> roundQuestions = new();
    private int currentIndex = -1;
    private int correctThisRound = 0;
    private float percentBefore = 0f;
    private bool improvedThisRound = false;

    // per-question shuffled mapping
    private int[] currentChoiceOrder = null;       // e.g., [2,0,3,1]
    private int currentShuffledCorrectIndex = -1;  // 0..3 after shuffle

    // timer
    private float remaining;
    private bool timerRunning;

    // pause/return
    private bool wasBackgrounded = false;

    private void Awake()
    {
        if (SaveSystem.Data == null) SaveSystem.Load();
    }

    private void Start()
    {
        // Panels off
        if (resultsPanel) resultsPanel.SetActive(false);
        if (failPanel)    failPanel.SetActive(false);
        if (toastPanel)   toastPanel.SetActive(false);
        if (quitConfirmPanel) quitConfirmPanel.SetActive(false);

        // Wire result/fail buttons
        if (btnToMenu)      { btnToMenu.onClick.RemoveAllListeners();      btnToMenu.onClick.AddListener(() => SceneManager.LoadScene("MainMenu")); }
        if (btnAdRetry)     { btnAdRetry.onClick.RemoveAllListeners();     btnAdRetry.onClick.AddListener(OnAdRetry); }
        if (btnQuitFail)    { btnQuitFail.onClick.RemoveAllListeners();    btnQuitFail.onClick.AddListener(() => SceneManager.LoadScene("LevelSelect")); }

        // Wire quit/top confirmation
        if (btnQuitTop)     { btnQuitTop.onClick.RemoveAllListeners();     btnQuitTop.onClick.AddListener(OpenQuitConfirm); }
        if (btnQuitConfirm) { btnQuitConfirm.onClick.RemoveAllListeners(); btnQuitConfirm.onClick.AddListener(QuitToLevelSelect); }
        if (btnQuitCancel)  { btnQuitCancel.onClick.RemoveAllListeners();  btnQuitCancel.onClick.AddListener(CloseQuitConfirm); }

        // Wire answer buttons
        WireAnswer(btnA, 0);
        WireAnswer(btnB, 1);
        WireAnswer(btnC, 2);
        WireAnswer(btnD, 3);

        // Load DB + choose set
        PrepareBankAndLevel();
        if (bank == null)
        {
            Debug.LogError("[QuizTimed] No CategoryBank for selected.");
            SceneManager.LoadScene("LevelSelect");
            return;
        }

        // previous % for improvement/toast
        int totalQs = TotalQuestionCount(bank);
        percentBefore = SaveSystem.GetPercent(bank.categoryId, totalQs);

        // Build round questions
        BuildRoundQuestions();

        // Top bar
        if (categoryTitle) categoryTitle.text = bank.categoryName;
        UpdateCoinsTopBar();
        SaveSystem.OnCoinsChanged += OnCoinsChanged;

        // First question
        NextQuestion();
    }

    private void OnDestroy()
    {
        SaveSystem.OnCoinsChanged -= OnCoinsChanged;
    }

    // ---------- Pause / Resume behavior ----------
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            wasBackgrounded = true;           // left the app
        }
        else
        {
            if (wasBackgrounded)
            {
                SceneManager.LoadScene("MainMenu"); // no refund
            }
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) wasBackgrounded = true;
    }

    // ---------- Coins HUD ----------
    private void OnCoinsChanged(int _) => UpdateCoinsTopBar();
    private void UpdateCoinsTopBar() { if (coinsText) coinsText.text = SaveSystem.GetCoins().ToString(); }

    // ---------- Answers ----------
    private void WireAnswer(Button btn, int index)
    {
        if (!btn) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnAnswerPressed(index));
    }

    // ---------- Data prep ----------
    private void PrepareBankAndLevel()
    {
        if (QuestionDB.Banks == null || QuestionDB.Banks.Count == 0)
            QuestionDB.LoadAllFromResources();

        var catId = QuizContext.SelectedCategoryId;
        if (string.IsNullOrEmpty(catId) || !QuestionDB.Banks.TryGetValue(catId, out bank))
        {
            Debug.LogError($"[QuizTimed] Category '{catId}' not found.");
            return;
        }

        int levelIndex = Mathf.Max(1, QuizContext.SelectedLevelIndex);
        if (bank.levels != null && bank.levels.Count > 0)
        {
            level = bank.levels.FirstOrDefault(l => l.levelIndex == levelIndex);
            if (level == null || level.questions == null || level.questions.Count == 0)
                Debug.LogError($"[QuizTimed] Level {levelIndex} has no questions in {bank.categoryId}.");
        }
        else
        {
            level = new LevelBank { levelIndex = 1, questions = new List<QuestionEntry>(bank.questions ?? new List<QuestionEntry>()) };
        }
    }

    private void BuildRoundQuestions()
    {
        roundQuestions.Clear();
        if (level == null || level.questions == null || level.questions.Count == 0) return;

        var prog = SaveSystem.GetProgress(bank.categoryId);

        // Prefer new questions; fill with already-correct if needed to reach 10.
        var notCorrect = level.questions.Where(q => !prog.correctQuestionIds.Contains(q.id)).ToList();

        var pool = new List<QuestionEntry>(notCorrect);
        if (pool.Count < questionsPerRound)
        {
            var filler = level.questions.Where(q => prog.correctQuestionIds.Contains(q.id)).ToList();
            Shuffle(filler);
            foreach (var q in filler)
            {
                if (pool.Count >= questionsPerRound) break;
                if (!pool.Contains(q)) pool.Add(q);
            }
        }

        Shuffle(pool);
        roundQuestions = pool.Take(Mathf.Min(questionsPerRound, pool.Count)).ToList();

        correctThisRound = 0;
        currentIndex = -1;
        UpdateCounter();
    }

    // ---------- Question flow ----------
    private void NextQuestion()
    {
        currentIndex++;
        if (currentIndex >= roundQuestions.Count) { EndRound(); return; }

        var q = roundQuestions[currentIndex];
        if (q == null) { EndRound(); return; }

        // Mark seen
        SaveSystem.MarkSeen(bank.categoryId, q.id, autoSave: false);
        SaveSystem.Save();

        // Text
        if (questionText) questionText.text = q.text;

        // Shuffle choices
        SetupShuffledChoices(q);

        // Reset & start timer (HARD reset UI first)
        ResetTimerUI();
        StartTimer();

        // Re-enable answers
        SetAnswersInteractable(true);
        UpdateCounter();
    }

    private void SetupShuffledChoices(QuestionEntry q)
    {
        int n = (q.choices != null) ? q.choices.Count : 0;
        if (n < 2)
        {
            currentChoiceOrder = new[] { 0, 1, 2, 3 };
            currentShuffledCorrectIndex = q.correctIndex;
            ApplyLabels(q);
            return;
        }

        currentChoiceOrder = Enumerable.Range(0, n).ToArray();
        Shuffle(currentChoiceOrder);
        currentShuffledCorrectIndex = System.Array.IndexOf(currentChoiceOrder, q.correctIndex);
        ApplyLabels(q);
    }

    private void ApplyLabels(QuestionEntry q)
    {
        string GetChoiceAt(int slot)
        {
            if (q.choices == null) return "";
            if (slot < 0 || slot >= currentChoiceOrder.Length) return "";
            int originalIndex = currentChoiceOrder[slot];
            if (originalIndex < 0 || originalIndex >= q.choices.Count) return "";
            return q.choices[originalIndex];
        }

        if (labelA) labelA.text = GetChoiceAt(0);
        if (labelB) labelB.text = GetChoiceAt(1);
        if (labelC) labelC.text = GetChoiceAt(2);
        if (labelD) labelD.text = GetChoiceAt(3);
    }

    private void SetAnswersInteractable(bool on)
    {
        if (btnA) btnA.interactable = on;
        if (btnB) btnB.interactable = on;
        if (btnC) btnC.interactable = on;
        if (btnD) btnD.interactable = on;
    }

    private void Update()
    {
        // Timer tick
        if (timerRunning)
        {
            remaining -= Time.deltaTime;

            if (timerFill)
            {
                float norm = Mathf.Clamp01(remaining / Mathf.Max(0.001f, secondsPerQuestion));
                timerFill.fillAmount = norm;
            }
            if (timerText)
            {
                timerText.text = Mathf.Max(0, Mathf.CeilToInt(remaining)).ToString();
            }

            if (remaining <= 0f)
            {
                timerRunning = false;
                // timeout counts as incorrect → move on
                SetAnswersInteractable(false);
                StartCoroutine(NextQuestionAfter(0.2f));
            }
        }
    }

    // ---- Timer helpers ----
    private void ResetTimerUI()
    {
        // Force fill to 1 and text to full seconds every time we show a new question
        if (timerFill)
        {
            timerFill.fillAmount = 1f;      // visually full
        }
        if (timerText)
        {
            int secs = Mathf.CeilToInt(Mathf.Max(1f, secondsPerQuestion));
            timerText.text = secs.ToString();
        }
        // Ensure Unity immediately lays out changes
        Canvas.ForceUpdateCanvases();
    }

    private void StartTimer()
    {
        remaining = Mathf.Max(1f, secondsPerQuestion);
        timerRunning = true;
    }

    private void StopTimer()
    {
        timerRunning = false;
    }

    private void UpdateCounter()
    {
        if (!questionCounterText) return;
        int shown = Mathf.Clamp(currentIndex + 1, 0, Mathf.Max(1, roundQuestions.Count));
        int total = Mathf.Max(1, roundQuestions.Count);
        questionCounterText.text = $"{shown}/{total}";
    }

    private void OnAnswerPressed(int pressedSlotIndex)
    {
        StopTimer();
        SetAnswersInteractable(false);

        var q = roundQuestions[currentIndex];
        bool correct = (pressedSlotIndex == currentShuffledCorrectIndex);
        if (correct)
        {
            correctThisRound++;
            SaveSystem.MarkCorrect(bank.categoryId, q.id, autoSave: true);
        }

        StartCoroutine(NextQuestionAfter(0.2f));
    }

    private IEnumerator NextQuestionAfter(float s)
    {
        yield return new WaitForSecondsRealtime(s);
        NextQuestion();
    }

    // ---------- End round ----------
    private void EndRound()
    {
        bool justUnlockedNext = false;
        bool levelNowComplete = SaveSystem.IsLevelComplete(bank.categoryId, level.levelIndex);

        if (correctThisRound >= unlockThreshold)
            justUnlockedNext = SaveSystem.UnlockNextLevelIfComplete(bank.categoryId, level.levelIndex);

        int totalQs = TotalQuestionCount(bank);
        float percentAfter = SaveSystem.GetPercent(bank.categoryId, totalQs);
        improvedThisRound = percentAfter > percentBefore + 0.0001f;

        if (correctThisRound < unlockThreshold)
        {
            if (resultsPanel) resultsPanel.SetActive(false);
            if (failPanel) { failPanel.SetActive(true); return; }
            SceneManager.LoadScene("LevelSelect"); return;
        }

        if (resultsPanel)
        {
            resultsPanel.SetActive(true);
            if (resultsText)
            {
                resultsText.text =
                    $"Score: {correctThisRound}/{questionsPerRound}\n" +
                    (levelNowComplete ? $"Level {level.levelIndex}: COMPLETED ✅"
                                      : $"Level {level.levelIndex}: keep going for 100%");

                if (justUnlockedNext) resultsText.text += $"\nNext level unlocked!";
            }

            if ((levelNowComplete || improvedThisRound) && toastPanel && toastText)
            {
                toastText.text = levelNowComplete
                    ? $"Level {level.levelIndex} 100% complete! 🎉"
                    : $"New best completion! 🎉";

                if (confetti) confetti.Play();
                StartCoroutine(ShowToast());
            }
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void OnAdRetry()
    {
        AdsManager.ShowRewarded(success =>
        {
            if (success) { SceneManager.LoadScene("Quiz"); }
            else { if (failPanel) failPanel.SetActive(false); }
        });
    }

    private IEnumerator ShowToast()
    {
        if (!toastPanel) yield break;
        toastPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(toastSeconds);
        toastPanel.SetActive(false);
    }

    // ---------- Quit confirm helpers ----------
    private void OpenQuitConfirm()
    {
        if (!quitConfirmPanel) return;
        if (!quitConfirmPanel.activeSelf) quitConfirmPanel.SetActive(true);

        if (quitConfirmCg)
        {
            quitConfirmCg.alpha = 0f;
            quitConfirmCg.blocksRaycasts = true;
            quitConfirmCg.interactable = true;
            StartCoroutine(FadeTo(quitConfirmCg, 1f, quitFade));
        }
    }

    private void CloseQuitConfirm()
    {
        if (!quitConfirmPanel) return;

        if (quitConfirmCg)
        {
            StartCoroutine(FadeTo(quitConfirmCg, 0f, quitFade, () =>
            {
                quitConfirmCg.blocksRaycasts = false;
                quitConfirmCg.interactable = false;
                quitConfirmPanel.SetActive(false);
            }));
        }
        else
        {
            quitConfirmPanel.SetActive(false);
        }
    }

    private void QuitToLevelSelect()
    {
        // No refund: cost was charged on entry via LevelButtonHook.
        SceneManager.LoadScene("LevelSelect");
    }

    private IEnumerator FadeTo(CanvasGroup cg, float target, float dur, System.Action onEnd = null)
    {
        float start = cg.alpha;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        cg.alpha = target;
        onEnd?.Invoke();
    }

    // ---------- Helpers ----------
    private static int TotalQuestionCount(CategoryBank b)
    {
        if (b == null) return 0;
        if (b.levels != null && b.levels.Count > 0)
            return b.levels.Sum(l => l.questions != null ? l.questions.Count : 0);
        return b.questions != null ? b.questions.Count : 0;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; --i)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static void Shuffle(int[] arr)
    {
        for (int i = arr.Length - 1; i > 0; --i)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }
}
