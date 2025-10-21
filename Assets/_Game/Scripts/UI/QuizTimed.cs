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
    [SerializeField] private Button btnQuit;

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

    private void Awake()
    {
        if (SaveSystem.Data == null) SaveSystem.Load();
    }

    private void Start()
    {
        if (resultsPanel) resultsPanel.SetActive(false);
        if (failPanel)    failPanel.SetActive(false);
        if (toastPanel)   toastPanel.SetActive(false);

        if (btnToMenu)  { btnToMenu.onClick.RemoveAllListeners();  btnToMenu.onClick.AddListener(() => SceneManager.LoadScene("MainMenu")); }
        if (btnAdRetry) { btnAdRetry.onClick.RemoveAllListeners(); btnAdRetry.onClick.AddListener(OnAdRetry); }
        if (btnQuit)    { btnQuit.onClick.RemoveAllListeners();    btnQuit.onClick.AddListener(() => SceneManager.LoadScene("LevelSelect")); }

        WireAnswer(btnA, 0);
        WireAnswer(btnB, 1);
        WireAnswer(btnC, 2);
        WireAnswer(btnD, 3);

        PrepareBankAndLevel();
        if (bank == null)
        {
            Debug.LogError("[QuizTimed] No CategoryBank for selected.");
            SceneManager.LoadScene("LevelSelect");
            return;
        }

        int totalQs = TotalQuestionCount(bank);
        percentBefore = SaveSystem.GetPercent(bank.categoryId, totalQs);

        BuildRoundQuestions();

        if (categoryTitle) categoryTitle.text = bank.categoryName;
        UpdateCoinsTopBar();
        SaveSystem.OnCoinsChanged += OnCoinsChanged;

        NextQuestion();
    }

    private void OnDestroy() => SaveSystem.OnCoinsChanged -= OnCoinsChanged;
    private void OnCoinsChanged(int _) => UpdateCoinsTopBar();
    private void UpdateCoinsTopBar() { if (coinsText) coinsText.text = SaveSystem.GetCoins().ToString(); }

    private void WireAnswer(Button btn, int index)
    {
        if (!btn) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnAnswerPressed(index));
    }

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

        // Prefer new (not-yet-correct) questions; fill with already-correct if needed to reach 10.
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

    private void NextQuestion()
    {
        currentIndex++;
        if (currentIndex >= roundQuestions.Count) { EndRound(); return; }

        var q = roundQuestions[currentIndex];
        if (q == null) { EndRound(); return; }

        // mark seen
        SaveSystem.MarkSeen(bank.categoryId, q.id, autoSave: false);
        SaveSystem.Save();

        // set question text
        if (questionText) questionText.text = q.text;

        // build shuffled choice order and compute shuffled correct index
        SetupShuffledChoices(q);

        // re-enable answers
        SetAnswersInteractable(true);
        UpdateCounter();
    }

    private void SetupShuffledChoices(QuestionEntry q)
    {
        // Build order 0..(n-1)
        int n = (q.choices != null) ? q.choices.Count : 0;
        if (n < 2)
        {
            // fallback: disable further
            currentChoiceOrder = new[] { 0, 1, 2, 3 };
            currentShuffledCorrectIndex = q.correctIndex;
            ApplyLabels(q);
            return;
        }

        // create order array and shuffle
        currentChoiceOrder = Enumerable.Range(0, n).ToArray();
        Shuffle(currentChoiceOrder);

        // find where the original correctIndex ended up after shuffle
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

    private void UpdateCounter()
    {
        if (!questionCounterText) return;
        int shown = Mathf.Clamp(currentIndex + 1, 0, Mathf.Max(1, roundQuestions.Count));
        int total = Mathf.Max(1, roundQuestions.Count);
        questionCounterText.text = $"{shown}/{total}";
    }

    private void OnAnswerPressed(int pressedSlotIndex)
    {
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

    // helpers
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

    // overload for int[]
    private static void Shuffle(int[] arr)
    {
        for (int i = arr.Length - 1; i > 0; --i)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }
}
