using UnityEngine;

public class Spinner : MonoBehaviour
{
    [SerializeField] private float speed = 150f;

    void Update()
    {
        transform.Rotate(0, 0, -speed * Time.deltaTime);
    }
}
