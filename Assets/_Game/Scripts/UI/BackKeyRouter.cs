using UnityEngine;
using UnityEngine.SceneManagement;

public class BackKeyRouter : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("CategoryScene");
        }
    }
}
