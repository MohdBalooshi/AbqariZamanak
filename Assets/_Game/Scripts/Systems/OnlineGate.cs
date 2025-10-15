using UnityEngine;
using UnityEngine.SceneManagement;

public class OnlineGate : MonoBehaviour
{
    void Start()
    {
        // TODO: add connectivity checks here if needed later
        QuestionDB.LoadAllFromResources();
        SceneManager.LoadScene("MainMenu");
    }
}
