using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

public class OnlineGate : MonoBehaviour
{
    [Header("Next Scene")]
    [SerializeField] private string nextScene = "MainMenu";

    [Header("Boot Options")]
    [Tooltip("If true, performs a quick internet connectivity check before continuing.")]
    [SerializeField] private bool requireInternet = false;

    [Tooltip("Minimum time (seconds) to keep the boot scene visible (nice for splash).")]
    [SerializeField] private float minSplashSeconds = 0.75f;

    private void Start()
    {
        StartCoroutine(BootSequence());
    }

    private IEnumerator BootSequence()
    {
        // 1) Load persistent data first
        SaveSystem.Load();

        // 2) Load all question banks from Resources/QuestionBanks
        QuestionDB.LoadAllFromResources();

        // Optional: keep splash visible for a short minimum time
        float t0 = Time.realtimeSinceStartup;

        // 3) Optional internet check (kept non-blocking for reliability)
        if (requireInternet)
            yield return StartCoroutine(CheckInternetQuick());

        // Enforce minimal splash time
        float elapsed = Time.realtimeSinceStartup - t0;
        if (elapsed < minSplashSeconds)
            yield return new WaitForSeconds(minSplashSeconds - elapsed);

        // 4) Go to the next scene
        if (Application.CanStreamedLevelBeLoaded(nextScene))
        {
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            Debug.LogError($"[OnlineGate] Scene '{nextScene}' is not in Build Settings or name is wrong.");
        }
    }

    private IEnumerator CheckInternetQuick()
    {
        using (var req = UnityWebRequest.Head("https://www.google.com"))
        {
            req.timeout = 5; // seconds
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool ok = req.result == UnityWebRequest.Result.Success;
#else
            bool ok = !req.isNetworkError && !req.isHttpError;
#endif

            if (!ok)
                Debug.LogWarning("[OnlineGate] Internet check failed—continuing anyway.");
        }
    }
}
