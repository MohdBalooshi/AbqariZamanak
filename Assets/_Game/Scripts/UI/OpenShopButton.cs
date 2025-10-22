using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OpenShopButton : MonoBehaviour
{
    private void Awake()
    {
        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            ShopPanelController.Show();
        });
    }
}
