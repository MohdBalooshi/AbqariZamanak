using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CategoryType", menuName = "Quiz/Category Type", order = 0)]
public class CategoryTypeAsset : ScriptableObject
{
    [Header("Identity")]
    public string typeId;           // e.g., "general", "anime"
    public string displayName;      // e.g., "General", "Anime"
    public Sprite typeIcon;         // (optional) not used by builder but free to use

    [Header("Categories (IDs must match your JSON categoryId)")]
    public List<string> categoryIds = new List<string>();
}
