using UnityEngine;
using UnityEngine.UI;

public class CategoryViewSwitcher : MonoBehaviour
{
    [SerializeField] private Button btnView1;
    [SerializeField] private Button btnView2;
    [SerializeField] private GameObject view1Root;
    [SerializeField] private GameObject view2Root;

    private void Awake()
    {
        btnView1.onClick.AddListener(() => Show(true));
        btnView2.onClick.AddListener(() => Show(false));
    }

    private void Show(bool view1)
    {
        view1Root.SetActive(view1);
        view2Root.SetActive(!view1);
    }
}
