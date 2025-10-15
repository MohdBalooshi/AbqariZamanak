using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private SceneFader fader;

    public void StartGame()      => StartCoroutine(fader.FadeOut("CategoryScene"));
    public void OpenAchievements()=> StartCoroutine(fader.FadeOut("Achievements"));
    public void OpenSettings()   => StartCoroutine(fader.FadeOut("Settings"));
    public void QuitGame()       => Application.Quit();
}
