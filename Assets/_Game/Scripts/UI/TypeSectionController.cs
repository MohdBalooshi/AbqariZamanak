using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TypeSectionController : MonoBehaviour
{
    [SerializeField] private TMP_Text header;
    [SerializeField] private Transform gridParent;

    public void SetHeader(string text)
    {
        if (header) header.text = text;
    }

    public void Populate(IEnumerable<(string id, string name, Sprite icon)> items,
                         CategoryGridItem itemPrefab)
    {
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        foreach (var it in items)
        {
            var item = Instantiate(itemPrefab, gridParent);
            item.Init(it.id, it.name, it.icon);
        }
    }
}
