using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class QuizTimed : MonoBehaviour
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
    [SerializeField] GameObject toastPanel;     // Root object (has CanvasGroup). Inactive by default.
    [SerializeField] TMP_Text toastText;        // Child TMP
    [SerializeField] float toastShowSeconds = 2.0f;

    [Header("Effects")]
    [SerializeField] ConfettiBurst confetti;    // Optional confetti

    private CategoryBank bank;
    private CategoryProgress progress;
    private List<QuestionEntry> remaining;      // questions NOT seen yet
    private int index = -1;                     // index into 'remaining'
    private int correctThisRound = 0;

    private float percentBefore = 0f;
    private bool improvedThisRound = false;

    void Start()
    {
        if (QuestionDB.Banks == null || QuestionDB.Banks.Count == 0) QuestionDB.LoadAllFromResources();
        if (SaveSystem.Data == null) SaveSystem.Load();

        if (string.IsNullOrEmpty(QuizContext.SelectedCategoryId))
            QuizContext.SelectedCategoryId = "general";

        bank = QuestionDB.Banks[QuizContext.SelectedCategoryId];
        categoryTitle.text = bank.categoryName;

        // Subscribe to coins event; init display
        SaveSystem.OnCoinsChanged += HandleCoinsChanged;
        HandleCoinsChanged(SaveSystem.Data.coins);

        // Load/compute progress + percent BEFORE
        progress = SaveSystem.GetProgress(bank.categoryId);
        percentBefore = SaveSystem.GetPercent(bank.categoryId, bank.questions != null ? bank.questions.Count : 0);

        // Build remaining pool: exclude already-seen questions
        remaining = (bank.questions ?? new List<QuestionEntry>())
                    .Where(q => !progress.seenQuestionIds.Contains(q.id))
                    .ToList();

        // Shuffle remaining so the order is fresh each time
        Shuffle(remaining);

        if (btnToMenu) btnToMenu.onClick.AddListener(() =>
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"));

        if (toastPanel) toastPanel.SetActive(false);

        // If nothing left, end immediately with a friendly message
        if (remaining.Count == 0)
        {
            EndRoundNoNewQuestions();
            return;
        }

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
        int maxThisRound = Mathf.Min(bank.questionsPerRound, remaining.Count);

        if (index >= maxThisRound)
        {
            EndRound();
            return;
        }

        var q = remaining[index];
        questionText.text = q.text;
        if (questionCounter) questionCounter.text = $"{index + 1}/{maxThisRound}";

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
            timer.OnTimeUp = () => OnAnswer(-1); // time-out counts as wrong but still 'seen'
            timer.ResetTimer();
        }
    }

    void OnAnswer(int chosenIndex)
    {
        var q = remaining[index];
        bool correct = (chosenIndex == q.correctIndex);

        // Mark progress: this question has now been SEEN
        progress.seenQuestionIds.Add(q.id);

        if (correct)
        {
            correctThisRound++;
            progress.correctQuestionIds.Add(q.id);
            SaveSystem.AddCoins(coinsPerCorrect); // saves + notifies listeners
        }

        // Persist progress after each question (seen/correct)
        SaveSystem.Save();

        // Disable buttons to avoid double-clicks
        foreach (var b in answerButtons) b.interactable = false;

        NextQuestion();
    }

    void EndRound()
    {
        float totalQs = (bank.questions != null) ? bank.questions.Count : 0f;
        float percentAfter = SaveSystem.GetPercent(bank.categoryId, (int)totalQs);
        improvedThisRound = percentAfter > percentBefore + 0.0001f;

        if (resultsPanel)
        {
            resultsPanel.SetActive(true);
            if (resultsText)
            {
                resultsText.text = $"Correct Answers: {correctThisRound}/{Mathf.Min(bank.questionsPerRound, remaining.Count)}";
                if (improvedThisRound)
                    resultsText.text += $"\nNew best completion: {percentAfter:0}%";
            }

            if (improvedThisRound && toastPanel && toastText)
            {
                toastText.text = $"New best completion: {percentAfter:0}% 🎉";
                if (confetti) confetti.Play();
                StartCoroutine(ShowToast());
            }
        }
        else
        {
            if (improvedThisRound && toastPanel && toastText)
            {
                toastText.text = $"New best completion: {percentAfter:0}% 🎉";
                if (confetti) confetti.Play();
                StartCoroutine(ShowToastThenMenu());
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
        }
    }

    // When there are no new questions left in this category
    void EndRoundNoNewQuestions()
    {
        if (resultsPanel)
        {
            resultsPanel.SetActive(true);
            if (resultsText)
                resultsText.text = "You've answered all available questions in this category!\nCheck back later for new ones.";
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

        // Fade in
        for (float t = 0; t < 0.2f; t += Time.unscaledDeltaTime)
        {
            cg.alpha = Mathf.Lerp(0f, 1f, t / 0.2f);
            yield return null;
        }
        cg.alpha = 1f;

        // Hold
        yield return new WaitForSecondsRealtime(toastShowSeconds);

        // Fade out
        for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            cg.alpha = Mathf.Lerp(1f, 0f, t / 0.3f);
            yield return null;
        }
        cg.alpha = 0f;
        toastPanel.SetActive(false);
    }

    IEnumerator ShowToastThenMenu()
    {
        yield return ShowToast();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    // Fisher–Yates shuffle
    static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
