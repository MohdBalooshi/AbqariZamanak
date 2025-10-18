using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Per-category progress:
/// - Which questions have been SEEN at least once
/// - Which questions have been answered CORRECTLY at least once
/// - Highest unlocked level (1+)
/// Uses Lists for serialization and HashSets at runtime for fast lookups.
/// </summary>
[Serializable]
public class CategoryProgress
{
    public string categoryId;

    // Serializable lists (Unity JsonUtility cannot handle HashSet)
    public List<string> correctList = new();
    public List<string> seenList    = new();

    // Runtime caches
    [NonSerialized] public HashSet<string> correctQuestionIds = new();
    [NonSerialized] public HashSet<string> seenQuestionIds    = new();

    /// <summary> Highest unlocked level for this category (1 unlocked by default). </summary>
    public int unlockedLevelMax = 1;

    public void SyncFromLists()
    {
        correctQuestionIds = new HashSet<string>(correctList ?? new List<string>());
        seenQuestionIds    = new HashSet<string>(seenList    ?? new List<string>());
        if (unlockedLevelMax < 1) unlockedLevelMax = 1;
    }

    public void SyncToLists()
    {
        correctList = correctQuestionIds?.ToList() ?? new List<string>();
        seenList    = seenQuestionIds?.ToList()    ?? new List<string>();
        if (unlockedLevelMax < 1) unlockedLevelMax = 1;
    }
}

[Serializable]
public class SaveData
{
    public int coins = 0;
    public List<CategoryProgress> categories = new();
}

/// <summary>
/// Central save API for the quiz game.
/// Keeps coins and per-category progress, with level gating helpers.
/// </summary>
public static class SaveSystem
{
    // Keep the same key so existing saves still work
    private const string KEY = "QUIZ_SAVE_V1";

    public static SaveData Data { get; private set; } = new SaveData();

    /// <summary> Fired whenever coin balance changes via AddCoins/SetCoins. </summary>
    public static event Action<int> OnCoinsChanged;

    // -----------------------
    // Core Load / Save
    // -----------------------
    public static void Load()
    {
        if (!PlayerPrefs.HasKey(KEY))
        {
            Data = new SaveData();
            Save(); // create fresh save on first run
            return;
        }

        var json = PlayerPrefs.GetString(KEY);
        var loaded = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<SaveData>(json);
        Data = loaded ?? new SaveData();

        // Rehydrate runtime HashSets
        if (Data.categories == null) Data.categories = new List<CategoryProgress>();
        foreach (var c in Data.categories)
            c?.SyncFromLists();
    }

    public static void Save()
    {
        if (Data == null) Data = new SaveData();

        // Push runtime HashSets back to serializable lists
        foreach (var c in Data.categories)
            c?.SyncToLists();

        var json = JsonUtility.ToJson(Data);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();

        // Immediately restore HashSets to keep both in sync in memory
        foreach (var c in Data.categories)
            c?.SyncFromLists();
    }

    /// <summary> Wipes all saved data. Use carefully. </summary>
    public static void DeleteAll()
    {
        PlayerPrefs.DeleteKey(KEY);
        Data = new SaveData();
        Save(); // recreate minimal structure
        OnCoinsChanged?.Invoke(Data.coins);
    }

    // -----------------------
    // Category helpers
    // -----------------------
    public static CategoryProgress GetProgress(string categoryId)
    {
        if (Data == null) Load();
        if (Data.categories == null) Data.categories = new List<CategoryProgress>();

        var p = Data.categories.Find(c => c.categoryId == categoryId);
        if (p == null)
        {
            p = new CategoryProgress { categoryId = categoryId, unlockedLevelMax = 1 };
            p.SyncFromLists();
            Data.categories.Add(p);
        }
        else
        {
            // Ensure sets are hydrated if loaded from JSON
            if (p.correctQuestionIds == null || p.seenQuestionIds == null)
                p.SyncFromLists();
            if (p.unlockedLevelMax < 1) p.unlockedLevelMax = 1;
        }
        return p;
    }

    /// <summary> Percentage of correctly answered questions across the whole category. </summary>
    public static float GetPercent(string categoryId, int totalQuestions)
    {
        var p = GetProgress(categoryId);
        if (totalQuestions <= 0) return 0f;
        return (p.correctQuestionIds.Count / (float)totalQuestions) * 100f;
    }

    /// <summary> Total questions in a category (sums all levels if present; otherwise legacy flat list). </summary>
    public static int GetTotalQuestionCount(string categoryId)
    {
        if (QuestionDB.Banks == null || QuestionDB.Banks.Count == 0) QuestionDB.LoadAllFromResources();
        if (!QuestionDB.Banks.TryGetValue(categoryId, out var bank)) return 0;

        if (bank.levels != null && bank.levels.Count > 0)
        {
            int total = 0;
            foreach (var lvl in bank.levels)
                total += (lvl.questions != null ? lvl.questions.Count : 0);
            return total;
        }
        return bank.questions != null ? bank.questions.Count : 0;
    }

    // -----------------------
    // Marking progress
    // -----------------------
    /// <summary> Mark a question as seen (first time encountered in a run). Saves immediately if autoSave is true. </summary>
    public static void MarkSeen(string categoryId, string questionId, bool autoSave = true)
    {
        var p = GetProgress(categoryId);
        p.seenQuestionIds.Add(questionId);
        if (autoSave) Save();
    }

    /// <summary> Mark a question as correctly answered. Saves immediately if autoSave is true. </summary>
    public static void MarkCorrect(string categoryId, string questionId, bool autoSave = true)
    {
        var p = GetProgress(categoryId);
        p.correctQuestionIds.Add(questionId);
        if (autoSave) Save();
    }

    // -----------------------
    // Coins
    // -----------------------
    public static void AddCoins(int amount)
    {
        if (Data == null) Load();
        long sum = (long)Data.coins + amount;
        Data.coins = (int)Mathf.Max(0, sum);
        Save();
        OnCoinsChanged?.Invoke(Data.coins);
    }

    public static void SetCoins(int value)
    {
        if (Data == null) Load();
        Data.coins = Mathf.Max(0, value);
        Save();
        OnCoinsChanged?.Invoke(Data.coins);
    }

    // -----------------------
    // Levels: gating & queries
    // -----------------------
    /// <summary> Highest unlocked level index (1..N). </summary>
    public static int GetUnlockedLevelCount(string categoryId)
    {
        return GetProgress(categoryId).unlockedLevelMax;
    }

    /// <summary> True if ALL questions in the given level have been answered correctly at least once. </summary>
    public static bool IsLevelComplete(string categoryId, int levelIndex)
    {
        if (!QuestionDB.Banks.TryGetValue(categoryId, out var bank)) return false;

        // Levels-aware
        if (bank.levels != null && bank.levels.Count > 0)
        {
            var lvl = bank.levels.FirstOrDefault(l => l.levelIndex == levelIndex);
            if (lvl == null || lvl.questions == null || lvl.questions.Count == 0) return false;

            var prog = GetProgress(categoryId);
            foreach (var q in lvl.questions)
                if (!prog.correctQuestionIds.Contains(q.id)) return false;

            return true;
        }

        // Legacy single-level bank: only level 1 exists
        if (levelIndex != 1) return false;
        var total = bank.questions != null ? bank.questions.Count : 0;
        var p = GetProgress(categoryId);
        return total > 0 && p.correctQuestionIds.Count >= total;
    }

    /// <summary>
    /// If the level is complete, unlock the next level for this category (up to total available).
    /// Returns true if newly unlocked.
    /// </summary>
    public static bool UnlockNextLevelIfComplete(string categoryId, int levelIndex)
    {
        if (!QuestionDB.Banks.TryGetValue(categoryId, out var bank)) return false;
        if (!IsLevelComplete(categoryId, levelIndex)) return false;

        int totalLevels = (bank.levels != null && bank.levels.Count > 0) ? bank.levels.Count : 1;
        var prog = GetProgress(categoryId);

        int desired = Mathf.Min(levelIndex + 1, totalLevels);
        if (desired > prog.unlockedLevelMax)
        {
            prog.unlockedLevelMax = desired;
            Save();
            return true;
        }
        return false;
    }

    /// <summary> Number of remaining (not yet correct) questions in a given level. </summary>
    public static int GetLevelRemainingCount(string categoryId, int levelIndex)
    {
        if (!QuestionDB.Banks.TryGetValue(categoryId, out var bank)) return 0;

        if (bank.levels != null && bank.levels.Count > 0)
        {
            var lvl = bank.levels.FirstOrDefault(l => l.levelIndex == levelIndex);
            if (lvl == null || lvl.questions == null) return 0;

            var p = GetProgress(categoryId);
            return lvl.questions.Count(q => !p.correctQuestionIds.Contains(q.id));
        }

        // Legacy level 1
        if (levelIndex != 1) return 0;
        var prog = GetProgress(categoryId);
        return (bank.questions ?? new List<QuestionEntry>()).Count(q => !prog.correctQuestionIds.Contains(q.id));
    }

    /// <summary> Force-unlock up to a specific level (debug / admin). Saves automatically. </summary>
    public static void ForceUnlockUpTo(string categoryId, int levelIndex)
    {
        var p = GetProgress(categoryId);
        p.unlockedLevelMax = Mathf.Max(p.unlockedLevelMax, Mathf.Max(1, levelIndex));
        Save();
    }

    // -----------------------
    // Maintenance / Debug
    // -----------------------
    /// <summary> Clear progress for one category (keeps coins). </summary>
    public static void ResetCategory(string categoryId, bool keepUnlockedLevelsAt1 = true)
    {
        var p = GetProgress(categoryId);
        p.correctQuestionIds.Clear();
        p.seenQuestionIds.Clear();
        if (keepUnlockedLevelsAt1) p.unlockedLevelMax = 1;
        Save();
    }

    /// <summary> Clear ALL categories' progress (keeps coins). </summary>
    public static void ResetAllProgress(bool keepCoins = true)
    {
        int coinsBackup = Data?.coins ?? 0;
        foreach (var c in Data.categories)
        {
            c.correctQuestionIds.Clear();
            c.seenQuestionIds.Clear();
            c.unlockedLevelMax = 1;
        }
        Save();
        if (keepCoins) SetCoins(coinsBackup);
    }
}
