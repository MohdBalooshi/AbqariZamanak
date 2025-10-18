using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Quiz : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] TMP_Text categoryTitle;
    [SerializeField] TMP_Text questionCounter;
    [SerializeField] TMP_Text coinsText;

    [Header("Center")]
    [SerializeField] TMP_Text questionText;
    [SerializeField] List<Button> answerButtons; // A,B,C,D

    [Header("Timer")]
    [SerializeField] QuestionTimer timer;
    [SerializeField] int coinsPerCorrect = 2;

    [Header("Results")]
    [SerializeField] GameObject resultsPanel;
    [SerializeField] TMP_Text resultsText;
    [SerializeField] Button btnToMenu;

    [Header("Toast")]
    [SerializeField] GameObject toastPanel;     // Root (CanvasGroup). Inactive by default.
    [SerializeField] TMP_Text toastText;        // Child TMP
    [SerializeField] float toastShowSeconds = 2.0f;

    [Header("Effects")]
    [SerializeField] ConfettiBurst confetti;    // Optional

    [Header("Round Rules")]
    [SerializeField] int questionsPerRound = 10;
    [SerializeField] int unlockThreshold = 8;   // score needed (>=) to unlock next level

    private CategoryBank bank;
    private LevelBank level;
    private CategoryProgress progress;

    private List<QuestionEntry> roundQuestions; // always exactly questionsPerRound (with padding)
    private int index = -1;
    private int correctThisRound = 0;

    private float percentBefore = 0f;
    private bool improvedThisRound = false;

    void Start()
    {
        if (QuestionDB.Banks == null || QuestionDB.Banks.Count == 0) QuestionDB.LoadAllFromResources();
        if (SaveSystem.Data == null) SaveSystem.Load();

        if (string.IsNullOrEmpty(QuizContext.SelectedCategoryId))
            QuizContext.SelectedCategoryId = "general";
        if (QuizContext.SelectedLevelIndex < 1)
            QuizContext.SelectedLevelIndex = 1;

        bank = QuestionDB.Banks[QuizContext.SelectedCategoryId];
        level = bank.levels?.FirstOrDefault(l => l.levelIndex == QuizContext.SelectedLevelIndex);
        if (level == null)
        {
            // Legacy support (no levels): treat whole bank as level 1
            level = new LevelBank { levelIndex = 1, questions = bank.questions ?? new List<QuestionEntry>() };
        }

        if (categoryTitle) categoryTitle.text = $"{bank.categoryName} — Level {level.levelIndex}";

        SaveSystem.OnCoinsChanged += HandleCoinsChanged;
        HandleCoinsChanged(SaveSystem.Data.coins);

        progress = SaveSystem.GetProgress(bank.categoryId);

        int totalQs = TotalQuestionCount(bank);
        percentBefore = SaveSystem.GetPercent(bank.categoryId, totalQs);

        // If level already fully complete, show message and bounce out
        if (SaveSystem.IsLevelComplete(bank.categoryId, level.levelIndex))
        {
            EndRoundAlreadyComplete();
            return;
        }

        // Build a 10-question round from "not-yet-correct", padded with already-correct if needed
        roundQuestions = BuildRound(level, progress, questionsPerRound);
        Shuffle(roundQuestions);

        if (roundQuestions.Count == 0)
        {
            EndRoundAlreadyComplete();
            return;
        }

        if (btnToMenu) btnToMenu.onClick.AddListener(() =>
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"));

        if (toastPanel) toastPanel.SetActive(false);

        NextQuestion();
    }

    void OnDestroy()
    {
        SaveSystem.OnCoinsChanged -= HandleCoinsChanged;
    }

    private void HandleCoinsChanged(int newCoins)
    {
        if (coinsText) coinsText.text = newCoins.ToString();
    }

    void NextQuestion()
    {
        index++;
        if (index >= roundQuestions.Count)
        {
            EndRound();
            return;
        }

        var q = roundQuestions[index];
        questionText.text = q.text;
        if (questionCounter) questionCounter.text = $"{index + 1}/{roundQuestions.Count}";

        for (int i = 0; i < answerButtons.Count; i++)
        {
            var b = answerButtons[i];
            var label = b.GetComponentInChildren<TMP_Text>();
            label.text = q.choices[i];
            int captured = i;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => OnAnswer(captured));
            b.interactable = true;
            b.gameObject.SetActive(true);
        }

        if (timer)
        {
            timer.OnTimeUp = () => OnAnswer(-1);
            timer.ResetTimer();
        }
    }

    void OnAnswer(int chosenIndex)
    {
        var q = roundQuestions[index];
        bool correctNow = (chosenIndex == q.correctIndex);

        // Always mark seen
        SaveSystem.MarkSeen(bank.categoryId, q.id, autoSave: false);

        // Mark correct only if correct (keeps cumulative completion)
        if (correctNow)
        {
            correctThisRound++;
            SaveSystem.MarkCorrect(bank.categoryId, q.id, autoSave: false);
            SaveSystem.AddCoins(coinsPerCorrect); // this saves & notifies
        }
        else
        {
            // if you want to do something on wrong answers, you can log here
        }

        // Persist seen/correct changes (coins already saved inside AddCoins)
        SaveSystem.Save();

        foreach (var b in answerButtons) b.interactable = false;

        NextQuestion();
    }

    void EndRound()
    {
        // Check if level is now fully complete (all 10 correct at least once)
        bool levelNowComplete = SaveSystem.IsLevelComplete(bank.categoryId, level.levelIndex);

        // Unlock next level if score reached threshold (>= unlockThreshold)
        if (correctThisRound >= unlockThreshold)
        {
            // This unlock doesn’t require full completion; we advance at least to next level
            SaveSystem.ForceUnlockUpTo(bank.categoryId, level.levelIndex + 1);
        }

        // Update overall percent for toast
        int totalQs = TotalQuestionCount(bank);
        float percentAfter = SaveSystem.GetPercent(bank.categoryId, totalQs);
        improvedThisRound = percentAfter > percentBefore + 0.0001f;

        // UI results
        if (resultsPanel)
        {
            resultsPanel.SetActive(true);
            if (resultsText)
            {
                resultsText.text =
                    $"Score this round: {correctThisRound}/{questionsPerRound}\n" +
                    (levelNowComplete
                        ? $"Level {level.levelIndex}: COMPLETED ✅"
                        : $"Level {level.levelIndex}: keep going for 100%");

                if (correctThisRound >= unlockThreshold)
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
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    void EndRoundAlreadyComplete()
    {
        if (resultsPanel)
        {
            resultsPanel.SetActive(true);
            if (resultsText)
                resultsText.text = $"Level {level.levelIndex} is already 100% complete.\nChoose another level.";
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    IEnumerator ShowToast()
    {
        toastPanel.SetActive(true);
        var cg = toastPanel.GetComponent<CanvasGroup>();
        if (!cg) cg = toastPanel.AddComponent<CanvasGroup>();

        for (float t = 0; t < 0.2f; t += Time.unscaledDeltaTime)
        {
            cg.alpha = Mathf.Lerp(0f, 1f, t / 0.2f);
            yield return null;
        }
        cg.alpha = 1f;

        yield return new WaitForSecondsRealtime(toastShowSeconds);

        for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            cg.alpha = Mathf.Lerp(1f, 0f, t / 0.3f);
            yield return null;
        }
        cg.alpha = 0f;
        toastPanel.SetActive(false);
    }

    static List<QuestionEntry> BuildRound(LevelBank level, CategoryProgress prog, int targetCount)
    {
        var all = level.questions ?? new List<QuestionEntry>();

        // MUST include all not-yet-correct questions first (these are the ones we want the player to clear)
        var notYetCorrect = all.Where(q => !prog.correctQuestionIds.Contains(q.id)).ToList();

        // Start with not-yet-correct
        var round = new List<QuestionEntry>(notYetCorrect);

        // If fewer than targetCount, pad with already-correct items (random, no duplicates)
        if (round.Count < targetCount)
        {
            var alreadyCorrect = all.Where(q => prog.correctQuestionIds.Contains(q.id)).ToList();
            Shuffle(alreadyCorrect);

            foreach (var q in alreadyCorrect)
            {
                if (round.Count >= targetCount) break;
                if (!round.Any(x => x.id == q.id)) round.Add(q);
            }
        }

        // If more than targetCount, randomly cut down (player has lots to clear; we’ll take 10)
        if (round.Count > targetCount)
        {
            Shuffle(round);
            round = round.Take(targetCount).ToList();
        }

        return round;
    }

    static int TotalQuestionCount(CategoryBank bank)
    {
        if (bank.levels != null && bank.levels.Count > 0)
        {
            int total = 0;
            foreach (var l in bank.levels)
                total += l.questions != null ? l.questions.Count : 0;
            return total;
        }
        return bank.questions != null ? bank.questions.Count : 0;
    }

    static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
