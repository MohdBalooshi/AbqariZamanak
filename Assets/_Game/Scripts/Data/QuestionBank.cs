using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One question entry with text, choices, correct answer, and difficulty.
/// </summary>
[System.Serializable]
public class QuestionEntry
{
    public string id;
    public string text;
    public List<string> choices;
    public int correctIndex;
    public int difficulty;
}

/// <summary>
/// A level block — each category can have multiple levels.
/// Each level holds a list of 10 (or more) questions.
/// </summary>
[System.Serializable]
public class LevelBlock
{
    public int levelIndex;
    public List<QuestionEntry> questions;
}

/// <summary>
/// A category (e.g. Sports, Science, etc.).
/// Can hold either one flat list of questions or multiple LevelBlocks.
/// </summary>
[System.Serializable]
public class QuestionBank
{
    public string categoryId;
    public string categoryName;
    public int questionsPerRound;
    public List<QuestionEntry> questions;    // used for simple categories
    public List<LevelBlock> levels;          // used for multi-level categories
}
