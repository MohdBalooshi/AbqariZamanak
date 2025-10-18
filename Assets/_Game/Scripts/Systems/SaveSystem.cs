using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class CategoryProgress
{
    public string categoryId;

    // Serializable lists
    public List<string> correctList = new();
    public List<string> seenList    = new();

    // Runtime sets
    [NonSerialized] public HashSet<string> correctQuestionIds = new();
    [NonSerialized] public HashSet<string> seenQuestionIds    = new();

    // Highest unlocked level (1 unlocked by default)
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

public static class SaveSystem
{
    private const string KEY = "QUIZ_SAVE_V1";
    public static SaveData Data { get; private set; } = new SaveData();

    public static event Action<int> OnCoinsChanged;

    public static void Load()
    {
        if (!PlayerPrefs.HasKey(KEY))
        {
            Data = new SaveData();
            Save(); // create fresh
            return;
        }

        var json = PlayerPrefs.GetString(KEY);
        var loaded = JsonUtility.FromJson<SaveData>(json);
        Data = loaded ?? new SaveData();

        foreach (var c in Data.categories)
            c?.SyncFromLists();
    }

    public static void Save()
    {
        foreach (var c in Data.categories)
            c?.SyncToLists();

        var json = JsonUtility.ToJson(Data);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();

        foreach (var c in Data.categories)
            c?.SyncFromLists();
    }

    public static CategoryProgress GetProgress(string categoryId)
    {
        var p = Data.categories.Find(c => c.categoryId == categoryId);
        if (p == null)
        {
            p = new CategoryProgress { categoryId = categoryId, unlockedLevelMax = 1 };
            p.SyncFromLists();
            Data.categories.Add(p);
        }
        else if (p.correctQuestionIds == null || p.seenQuestionIds == null)
        {
            p.SyncFromLists();
        }
        if (p.unlockedLevelMax < 1) p.unlockedLevelMax = 1;
        return p;
    }

    public static float GetPercent(string categoryId, int totalQuestions)
    {
        var p = GetProgress(categoryId);
        if (totalQuestions <= 0) return 0f;
        return (p.correctQuestionIds.Count / (float)totalQuestions) * 100f;
    }

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

    // ---- Level helpers ----

    /// <summary> How many levels are unlocked (1..max). </summary>
    public static int GetUnlockedLevelCount(string categoryId)
    {
        return GetProgress(categoryId).unlockedLevelMax;
    }

    /// <summary>
    /// Check if all questions in a given level are already correctly answered at least once.
    /// </summary>
    public static bool IsLevelComplete(string categoryId, int levelIndex)
    {
        if (!QuestionDB.Banks.TryGetValue(categoryId, out var bank)) return false;
        var lvl = bank.levels?.Find(l => l.levelIndex == levelIndex);
        if (lvl == null || lvl.questions == null || lvl.questions.Count == 0) return false;
        var prog = GetProgress(categoryId);
        foreach (var q in lvl.questions)
            if (!prog.correctQuestionIds.Contains(q.id)) return false;
        return true;
    }

    /// <summary>
    /// If the current level is complete, unlock the next one (up to existing levels).
    /// Returns true if something was unlocked.
    /// </summary>
    public static bool UnlockNextLevelIfComplete(string categoryId, int levelIndex)
    {
        if (!QuestionDB.Banks.TryGetValue(categoryId, out var bank)) return false;
        if (!IsLevelComplete(categoryId, levelIndex)) return false;

        var prog = GetProgress(categoryId);
        int totalLevels = bank.levels != null ? bank.levels.Count : 1;
        int desired = Mathf.Min(levelIndex + 1, totalLevels);

        if (desired > prog.unlockedLevelMax)
        {
            prog.unlockedLevelMax = desired;
            Save();
            return true;
        }
        return false;
    }
}
