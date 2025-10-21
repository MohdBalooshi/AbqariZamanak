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
    [SerializeField] private TMP_Text coinsText; // optional live coin display

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
    [SerializeField] private int questionsPerRound = 10;   // how many questions in one play
    [SerializeField] private int unlockThreshold   = 8;    // min correct to count as success / unlock

    [Header("Results UI")]
    [SerializeField] private GameObject resultsPanel;      // inactive by default
    [SerializeField] private TMP_Text resultsText;
    [SerializeField] private Button btnToMenu;

    [Header("Failure UI")]
    [SerializeField] private GameObject failPanel;         // inactive by default
    [SerializeField] private Button btnAdRetry;            // watch ad → retry free
    [SerializeField] private Button btnQuit;               // back to LevelSelect

    [Header("Feedback")]
    [SerializeField] private ParticleSystem confetti;      // optional confetti on improvement/complete
    [SerializeField] private GameObject toastPanel;        // optional small “toast” panel
    [SerializeField] private TMP_Text toastText;           // optional toast text
    [SerializeField] private float toastSeconds = 1.4f;

    // -------- Internal state --------
    private CategoryBank bank;                 // your DB type
    private LevelBank level;                   // <- use LevelBank (not LevelBlock)
    private List<QuestionEntry> roundQuestions = new();
    private int currentIndex = -1; // index in roundQuestions
    private int correctThisRound = 0;
    private float percentBefore = 0f;
    private bool improvedThisRound = false;

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

        // Wire results/fail buttons
        if (btnToMenu)  { btnToMenu.onClick.RemoveAllListeners();  btnToMenu.onClick.AddListener(() => SceneManager.LoadScene("MainMenu")); }
        if (btnAdRetry) { btnAdRetry.onClick.RemoveAllListeners(); btnAdRetry.onClick.AddListener(OnAdRetry); }
        if (btnQuit)    { btnQuit.onClick.RemoveAllListeners();    btnQuit.onClick.AddListener(() => SceneManager.LoadScene("LevelSelect")); }

        // Wire answer buttons
        WireAnswer(btnA, 0);
        WireAnswer(btnB, 1);
        WireAnswer(btnC, 2);
        WireAnswer(btnD, 3);

        // Load DB + choose set
        PrepareBankAndLevel();
        if (bank == null)
        {
            Debug.LogError("[QuizTimed] No CategoryBank found for selected category.");
            SceneManager.LoadScene("LevelSelect");
            return;
        }

        // Pre-calc previous % before playing (for toast/improvement)
        int totalQs = TotalQuestionCount(bank);
        percentBefore = SaveSystem.GetPercent(bank.categoryId, totalQs);

        // Build this round’s questions
        BuildRoundQuestions();

        // Update top bar
        if (categoryTitle) categoryTitle.text = bank.categoryName;
        UpdateCoinsTopBar();
        SaveSystem.OnCoinsChanged += OnCoinsChanged;

        // Start first question
        NextQuestion();
    }

    private void OnDestroy()
    {
        SaveSystem.OnCoinsChanged -= OnCoinsChanged;
    }

    private void OnCoinsChanged(int newCoins) => UpdateCoinsTopBar();

    private void UpdateCoinsTopBar()
    {
        if (coinsText) coinsText.text = SaveSystem.GetCoins().ToString();
    }

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
            Debug.LogError($"[QuizTimed] Category '{catId}' not found in QuestionDB.");
            return;
        }

        int levelIndex = Mathf.Max(1, QuizContext.SelectedLevelIndex);

        if (bank.levels != null && bank.levels.Count > 0)
        {
            // bank.levels is List<LevelBank>
            level = bank.levels.FirstOrDefault(l => l.levelIndex == levelIndex);
            if (level == null || level.questions == null || level.questions.Count == 0)
            {
                Debug.LogError($"[QuizTimed] Level {levelIndex} has no questions in category {bank.categoryId}.");
            }
        }
        else
        {
            // Single-bucket category (no level blocks) → treat as level 1
            level = new LevelBank
            {
                levelIndex = 1,
                questions = new List<QuestionEntry>(bank.questions ?? new List<QuestionEntry>())
            };
        }
    }

    private void BuildRoundQuestions()
    {
        roundQuestions.Clear();
        if (level == null || level.questions == null || level.questions.Count == 0) return;

        // Player progress set
        var prog = SaveSystem.GetProgress(bank.categoryId);

        // 1) Prefer not-yet-correct questions in this level
        var notCorrect = level.questions.Where(q => !prog.correctQuestionIds.Contains(q.id)).ToList();

        // 2) If not enough, fill from the rest (already-correct) so the round can still run
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

        // Shuffle final pool & take N
        Shuffle(pool);
        roundQuestions = pool.Take(Mathf.Min(questionsPerRound, pool.Count)).ToList();
        correctThisRound = 0;
        currentIndex = -1;

        // Update initial counter
        UpdateCounter();
    }

    private void NextQuestion()
    {
        currentIndex++;
        if (currentIndex >= roundQuestions.Count)
        {
            EndRound();
            return;
        }

        var q = roundQuestions[currentIndex];
        if (q == null)
        {
            EndRound();
            return;
        }

        // Mark seen
        SaveSystem.MarkSeen(bank.categoryId, q.id, autoSave: false);
        SaveSystem.Save();

        // Fill UI
        if (questionText) questionText.text = q.text;

        if (labelA) labelA.text = q.choices != null && q.choices.Count > 0 ? q.choices[0] : "";
        if (labelB) labelB.text = q.choices != null && q.choices.Count > 1 ? q.choices[1] : "";
        if (labelC) labelC.text = q.choices != null && q.choices.Count > 2 ? q.choices[2] : "";
        if (labelD) labelD.text = q.choices != null && q.choices.Count > 3 ? q.choices[3] : "";

        // Re-enable buttons
        SetAnswersInteractable(true);

        UpdateCounter();
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

    private void OnAnswerPressed(int choiceIndex)
    {
        SetAnswersInteractable(false);

        var q = roundQuestions[currentIndex];
        bool correct = (q.correctIndex == choiceIndex);

        if (correct)
        {
            correctThisRound++;
            SaveSystem.MarkCorrect(bank.categoryId, q.id, autoSave: true);
        }

        StartCoroutine(NextQuestionAfter(0.2f));
    }

    private IEnumerator NextQuestionAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        NextQuestion();
    }

    private void EndRound()
    {
        // Unlock next level if reached threshold
        bool justUnlockedNext = false;
        bool levelNowComplete = SaveSystem.IsLevelComplete(bank.categoryId, level.levelIndex);

        if (correctThisRound >= unlockThreshold)
            justUnlockedNext = SaveSystem.UnlockNextLevelIfComplete(bank.categoryId, level.levelIndex);

        // Improvement / % calc
        int totalQs = TotalQuestionCount(bank);
        float percentAfter = SaveSystem.GetPercent(bank.categoryId, totalQs);
        improvedThisRound = percentAfter > percentBefore + 0.0001f;

        // If failed to reach threshold → show Fail Panel (ad retry)
        if (correctThisRound < unlockThreshold)
        {
            if (resultsPanel) resultsPanel.SetActive(false);
            if (failPanel)
            {
                failPanel.SetActive(true);
                return;
            }

            // Fallback (no fail panel wired):
            SceneManager.LoadScene("LevelSelect");
            return;
        }

        // Success → show results
        if (resultsPanel)
        {
            resultsPanel.SetActive(true);
            if (resultsText)
            {
                resultsText.text =
                    $"Score: {correctThisRound}/{questionsPerRound}\n" +
                    (levelNowComplete
                        ? $"Level {level.levelIndex}: COMPLETED ✅"
                        : $"Level {level.levelIndex}: keep going for 100%");

                if (justUnlockedNext)
                    resultsText.text += $"\nNext level unlocked!";

                if (improvedThisRound)
                    resultsText.text += $"\nNew best completion: {percentAfter:0}%";
            }

            if ((levelNowComplete || improvedThisRound) && toastPanel && toastText)
            {
                toastText.text = levelNowComplete
                    ? $"Level {level.levelIndex} 100% complete! 🎉"
                    : $"New best completion: {percentAfter:0}% 🎉";

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
            if (success)
            {
                SceneManager.LoadScene("Quiz");
            }
            else
            {
                if (failPanel) failPanel.SetActive(false);
            }
        });
    }

    private IEnumerator ShowToast()
    {
        if (!toastPanel) yield break;
        toastPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(toastSeconds);
        toastPanel.SetActive(false);
    }

    // ------------ Helpers ------------
    private static int TotalQuestionCount(CategoryBank b)
    {
        if (b == null) return 0;
        if (b.levels != null && b.levels.Count > 0)
        {
            int total = 0;
            foreach (var lvl in b.levels)
                total += (lvl.questions != null ? lvl.questions.Count : 0);
            return total;
        }
        return b.questions != null ? b.questions.Count : 0;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        // Fisher–Yates
        for (int i = list.Count - 1; i > 0; --i)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
