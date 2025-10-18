using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestionTimer : MonoBehaviour
{
    [SerializeField] Image fillBar;         // TimerBar/Fill (Image Type = Filled Horizontal)
    [SerializeField] TMP_Text timerLabel;   // optional
    [SerializeField] float maxSeconds = 20f;

    float remaining; bool running;
    public System.Action OnTimeUp;

    void OnEnable(){ ResetTimer(); }
    public void ResetTimer(){ remaining = maxSeconds; running = true; UpdateUI(); }
    public void StopTimer(){ running = false; }

    void Update() {
        if (!running) return;
        remaining -= Time.deltaTime;
        if (remaining <= 0f) { remaining = 0f; running = false; UpdateUI(); OnTimeUp?.Invoke(); }
        else UpdateUI();
    }

    void UpdateUI() {
        if (fillBar) fillBar.fillAmount = Mathf.InverseLerp(0, maxSeconds, remaining);
        if (timerLabel) timerLabel.text = Mathf.CeilToInt(remaining).ToString();
    }
}
