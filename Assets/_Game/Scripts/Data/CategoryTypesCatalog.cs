using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CategoryTypesCatalog", menuName = "Quiz/Category Types Catalog", order = 1)]
public class CategoryTypesCatalog : ScriptableObject
{
    public List<CategoryTypeAsset> types = new List<CategoryTypeAsset>();
}
