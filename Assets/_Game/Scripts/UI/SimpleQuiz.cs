using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class SimpleQuiz : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] TMP_Text categoryTitle;
    [SerializeField] TMP_Text questionCounter;

    [Header("Center")]
    [SerializeField] TMP_Text questionText;
    [SerializeField] List<Button> answerButtons; // A,B,C,D

    private CategoryBank bank;
    private int index = -1;

    void Start()
    {
        if (QuestionDB.Banks == null || QuestionDB.Banks.Count == 0)
            QuestionDB.LoadAllFromResources();

        if (string.IsNullOrEmpty(QuizContext.SelectedCategoryId))
            QuizContext.SelectedCategoryId = "general"; // default fallback

        bank = QuestionDB.Banks[QuizContext.SelectedCategoryId];
        categoryTitle.text = bank.categoryName;

        NextQuestion();
    }

    void NextQuestion()
    {
        index++;
        if (index >= bank.questions.Count || index >= bank.questionsPerRound)
        {
            // End of round → go back for now
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            return;
        }

        var q = bank.questions[index];
        questionText.text = q.text;
        if (questionCounter) questionCounter.text = $"{index + 1}/{Mathf.Min(bank.questionsPerRound, bank.questions.Count)}";

        for (int i = 0; i < answerButtons.Count; i++)
        {
            var b = answerButtons[i];
            var label = b.GetComponentInChildren<TMP_Text>();
            label.text = q.choices[i];

            int captured = i;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => OnAnswer(captured == q.correctIndex));
        }
    }

    void OnAnswer(bool correct)
    {
        // (Later: add feedback, coins, ads, etc.)
        NextQuestion();
    }
}
