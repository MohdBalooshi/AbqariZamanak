using System;
using System.Collections.Generic;
using UnityEngine;


// NEW: one level = 10 questions
[Serializable] public class LevelBank
{
    public int levelIndex = 1;                 // 1..5
    public List<QuestionEntry> questions = new();
}

[Serializable] public class CategoryBank
{
    public string categoryId;                  // e.g., "general"
    public string categoryName;                // e.g., "General Knowledge"

    // NEW preferred structure
    public List<LevelBank> levels = new();

    // BACKWARD COMPAT: support old flat format
    public int questionsPerRound = 10;
    public List<QuestionEntry> questions;      // legacy; treated as level 1
}

public static class QuestionDB
{
    private static Dictionary<string, CategoryBank> _banks = new();
    public static IReadOnlyDictionary<string, CategoryBank> Banks => _banks;

    public static void LoadAllFromResources()
    {
        _banks.Clear();
        var all = Resources.LoadAll<TextAsset>("QuestionBanks");
        foreach (var ta in all)
        {
            var raw = JsonUtility.FromJson<CategoryBank>(ta.text);
            if (raw == null || string.IsNullOrEmpty(raw.categoryId))
                continue;

            // BACKWARD COMPAT: up-convert legacy questions list to levels[0]
            if ((raw.levels == null || raw.levels.Count == 0) && raw.questions != null && raw.questions.Count > 0)
            {
                raw.levels = new List<LevelBank>
                {
                    new LevelBank { levelIndex = 1, questions = new List<QuestionEntry>(raw.questions) }
                };
            }

            // Normalize: ensure exactly 5 levels exist if you want a fixed structure
            // (Optional) — we won’t auto-create empty levels to avoid confusion.

            _banks[raw.categoryId] = raw;
        }
        Debug.Log($"[QuestionDB] Loaded {_banks.Count} categories (levels aware)");
    }
}

public static class QuizContext
{
    public static string SelectedCategoryId;
    public static int SelectedLevelIndex = 1;   // 1..5
}
