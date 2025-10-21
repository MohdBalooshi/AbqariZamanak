using System;
using UnityEngine;

public static class AdsManager
{
    /// <summary>
    /// Simulates showing a rewarded ad.
    /// In production, replace this with your ad SDK’s real callback.
    /// </summary>
    public static void ShowRewarded(Action<bool> onComplete)
    {
        Debug.Log("[AdsManager] Simulating rewarded ad...");

        // Simulate a 2-second delay before calling success = true
        InstanceHelper.Instance.StartCoroutine(SimulateAd(onComplete));
    }

    private static System.Collections.IEnumerator SimulateAd(Action<bool> onComplete)
    {
        yield return new WaitForSeconds(2f);
        Debug.Log("[AdsManager] Fake ad complete → reward granted.");
        onComplete?.Invoke(true);
    }
}

/// <summary>
/// Small helper MonoBehaviour to run coroutines from static classes.
/// </summary>
public class InstanceHelper : MonoBehaviour
{
    private static InstanceHelper _instance;
    public static InstanceHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[InstanceHelper]");
                _instance = go.AddComponent<InstanceHelper>();
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
}
