using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class InternetChecker : MonoBehaviour
{
    public static IEnumerator CheckConnection(System.Action<bool> callback)
    {
        using (UnityWebRequest request = new UnityWebRequest("https://www.google.com"))
        {
            request.timeout = 3;
            yield return request.SendWebRequest();

            bool isOnline = !request.result.ToString().Contains("Error");
            callback?.Invoke(isOnline);
        }
    }
}
