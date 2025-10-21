using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class CategoryProgress
{
    public string categoryId;
    public List<string> correctList = new();
    public List<string> seenList    = new();

    [NonSerialized] public HashSet<string> correctQuestionIds = new();
    [NonSerialized] public HashSet<string> seenQuestionIds    = new();

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
public class GameSettings
{
    public float musicVolume = 0.8f;
    public float sfxVolume   = 1.0f;
    public bool  vibrate     = true;
    public string language   = "en";
}

[Serializable]
public class SaveData
{
    public int coins = 0;
    public string playerName = "";
    public bool signupBonusClaimed = false;   // NEW

    public GameSettings settings = new();
    public List<CategoryProgress> categories = new();
}

public static class SaveSystem
{
    private const string KEY = "QUIZ_SAVE_V1";

    public static SaveData Data { get; private set; } = new SaveData();

    public static event Action<int> OnCoinsChanged;

    // ---------------- Core ----------------
    public static void Load()
    {
        if (!PlayerPrefs.HasKey(KEY))
        {
            Data = new SaveData();
            Save();
            return;
        }

        var json = PlayerPrefs.GetString(KEY);
        var loaded = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<SaveData>(json);
        Data = loaded ?? new SaveData();

        if (Data.categories == null) Data.categories = new List<CategoryProgress>();
        foreach (var c in Data.categories) c?.SyncFromLists();
        if (Data.settings == null) Data.settings = new GameSettings();
        if (Data.playerName == null) Data.playerName = "";
    }

    public static void Save()
    {
        if (Data == null) Data = new SaveData();
        foreach (var c in Data.categories) c?.SyncToLists();

        var json = JsonUtility.ToJson(Data);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();

        foreach (var c in Data.categories) c?.SyncFromLists();
    }

    public static void DeleteAll()
    {
        PlayerPrefs.DeleteKey(KEY);
        Data = new SaveData();
        Save();
        OnCoinsChanged?.Invoke(Data.coins);
    }

    // ------------- Economy -------------
    public static int GetCoins() => Data?.coins ?? 0;

    public static bool HasCoins(int amount)
    {
        if (Data == null) Load();
        return Data.coins >= Mathf.Max(0, amount);
    }

    public static bool TrySpend(int amount)
    {
        if (Data == null) Load();
        amount = Mathf.Max(0, amount);
        if (Data.coins < amount) return false;
        Data.coins -= amount;
        Save();
        OnCoinsChanged?.Invoke(Data.coins);
        return true;
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

    public static void GrantSignupBonusOnce(int bonus)
    {
        if (Data == null) Load();
        if (Data.signupBonusClaimed) return;
        if (bonus > 0) AddCoins(bonus);
        Data.signupBonusClaimed = true;
        Save();
    }

    // ------------- Categories / levels -------------
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
            if (p.correctQuestionIds == null || p.seenQuestionIds == null) p.SyncFromLists();
            if (p.unlockedLevelMax < 1) p.unlockedLevelMax = 1;
        }
        return p;
    }

    public static float GetPercent(string categoryId, int totalQuestions)
    {
        var p = GetProgress(categoryId);
        if (totalQuestions <= 0) return 0f;
        return (p.correctQuestionIds.Count / (float)totalQuestions) * 100f;
    }

    public static int GetTotalQuestionCount(string categoryId)
    {
        if (QuestionDB.Banks == null || QuestionDB.Banks.Count == 0) QuestionDB.LoadAllFromResources();
        if (!QuestionDB.Banks.TryGetValue(categoryId, out var bank)) return 0;

        if (bank.levels != null && bank.levels.Count > 0)
        {
            int total = 0;
            foreach (var lvl in bank.levels) total += (lvl.questions != null ? lvl.questions.Count : 0);
            return total;
        }
        return bank.questions != null ? bank.questions.Count : 0;
    }

    public static void MarkSeen(string categoryId, string questionId, bool autoSave = true)
    {
        var p = GetProgress(categoryId);
        p.seenQuestionIds.Add(questionId);
        if (autoSave) Save();
    }

    public static void MarkCorrect(string categoryId, string questionId, bool autoSave = true)
    {
        var p = GetProgress(categoryId);
        p.correctQuestionIds.Add(questionId);
        if (autoSave) Save();
    }

    public static int GetUnlockedLevelCount(string categoryId)
    {
        return GetProgress(categoryId).unlockedLevelMax;
    }

    public static bool IsLevelComplete(string categoryId, int levelIndex)
    {
        if (!QuestionDB.Banks.TryGetValue(categoryId, out var bank)) return false;

        if (bank.levels != null && bank.levels.Count > 0)
        {
            var lvl = bank.levels.FirstOrDefault(l => l.levelIndex == levelIndex);
            if (lvl == null || lvl.questions == null || lvl.questions.Count == 0) return false;

            var prog = GetProgress(categoryId);
            foreach (var q in lvl.questions)
                if (!prog.correctQuestionIds.Contains(q.id)) return false;

            return true;
        }

        if (levelIndex != 1) return false;
        var total = bank.questions != null ? bank.questions.Count : 0;
        var p = GetProgress(categoryId);
        return total > 0 && p.correctQuestionIds.Count >= total;
    }

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

        if (levelIndex != 1) return 0;
        var prog = GetProgress(categoryId);
        return (bank.questions ?? new List<QuestionEntry>())
            .Count(q => !prog.correctQuestionIds.Contains(q.id));
    }

    public static void ForceUnlockUpTo(string categoryId, int levelIndex)
    {
        var p = GetProgress(categoryId);
        p.unlockedLevelMax = Mathf.Max(p.unlockedLevelMax, Mathf.Max(1, levelIndex));
        Save();
    }

    public static void ResetCategory(string categoryId, bool keepUnlockedLevelsAt1 = true)
    {
        var p = GetProgress(categoryId);
        p.correctQuestionIds.Clear();
        p.seenQuestionIds.Clear();
        if (keepUnlockedLevelsAt1) p.unlockedLevelMax = 1;
        Save();
    }

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

    // ------------- Player/Profile -------------
    public static string GetPlayerName() => Data?.playerName ?? "";
    public static void SetPlayerName(string name)
    {
        if (Data == null) Load();
        Data.playerName = name ?? "";
        Save();
    }

    public static bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(Data?.playerName);
    }

    public static GameSettings GetSettings() => Data?.settings ?? new GameSettings();

    public static void SetSettings(GameSettings s)
    {
        if (Data == null) Load();
        Data.settings = s ?? new GameSettings();
        Save();
    }

    public static void SetVolumes(float music, float sfx)
    {
        if (Data == null) Load();
        Data.settings.musicVolume = Mathf.Clamp01(music);
        Data.settings.sfxVolume   = Mathf.Clamp01(sfx);
        Save();
    }

    public static void SetVibrate(bool on)
    {
        if (Data == null) Load();
        Data.settings.vibrate = on;
        Save();
    }

    public static void SetLanguage(string lang)
    {
        if (Data == null) Load();
        Data.settings.language = string.IsNullOrEmpty(lang) ? "en" : lang;
        Save();
    }
}
