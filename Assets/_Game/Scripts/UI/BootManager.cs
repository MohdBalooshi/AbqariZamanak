using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class BootManager : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;

    private void Start()
    {
        statusText.text = "Connecting....";
        StartCoroutine(InternetChecker.CheckConnection(OnCheckComplete));
    }

    private void OnCheckComplete(bool online)
    {
        if (online)
        {
            statusText.text = "Connection made";
            Invoke(nameof(LoadMenu), 1.5f);
        }
        else
        {
            statusText.text = "No internet connection found...";
            Invoke(nameof(Retry), 2f);
        }
    }

    private void Retry()
    {
        statusText.text = "Reconnecting....";
        StartCoroutine(InternetChecker.CheckConnection(OnCheckComplete));
    }

    private void LoadMenu()
    {
       SceneManager.LoadScene("MainMenu");
    }
}
