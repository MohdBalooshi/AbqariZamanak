using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class QuestionEntry
{
    public string id;
    public string text;
    public List<string> choices;   // 4 options
    public int correctIndex;       // 0..3
    public int difficulty;         // optional
}

[Serializable] public class CategoryBank
{
    public string categoryId;      // e.g., "general"
    public string categoryName;    // e.g., "General Knowledge"
    public int questionsPerRound = 10;
    public List<QuestionEntry> questions;
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
            var bank = JsonUtility.FromJson<CategoryBank>(ta.text);
            if (bank != null && !string.IsNullOrEmpty(bank.categoryId))
                _banks[bank.categoryId] = bank;
        }
        Debug.Log($"[QuestionDB] Loaded {_banks.Count} categories");
    }
}

public static class QuizContext
{
    public static string SelectedCategoryId;   // set by Category Scene, read by Quiz Scene
}
