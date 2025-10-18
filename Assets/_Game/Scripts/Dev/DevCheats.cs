using UnityEngine;

public class DevCheats : MonoBehaviour
{
    [Header("Category to reset")]
    public string categoryId = "detective_conan";

    [ContextMenu("Reset Category Progress")]
    public void ResetCategory()
    {
        SaveSystem.Load();
        SaveSystem.ResetCategory(categoryId);
        Debug.Log($"[DevCheats] Reset progress for '{categoryId}'.");
    }

    [ContextMenu("Delete ALL Save Data")]
    public void DeleteAll()
    {
        SaveSystem.DeleteAll();
        Debug.Log("[DevCheats] Deleted ALL save data.");
    }

    [ContextMenu("Force-Unlock Up To Level 5")]
    public void ForceUnlock5()
    {
        SaveSystem.Load();
        SaveSystem.ForceUnlockUpTo(categoryId, 5);
        Debug.Log($"[DevCheats] Force-unlocked up to L5 for '{categoryId}'.");
    }
}
