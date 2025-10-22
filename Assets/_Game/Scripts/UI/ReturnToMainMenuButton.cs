using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Button))]
public class ReturnToMainMenuButton : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Confirmation")]
    [SerializeField] private bool askBeforeExit = true;
    [SerializeField] private GameObject confirmPanel;        // <- assign the ROOT overlay panel (inactive by default)
    [SerializeField] private CanvasGroup confirmCanvasGroup; // optional; assign if you want fade
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private Button btnConfirm;              // optional wiring from inspector
    [SerializeField] private Button btnCancel;               // optional wiring from inspector

    private Button _button;
    private Coroutine _fadeRoutine;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnPressed);

        // If confirm panel has its own buttons, wire them (optional)
        if (btnConfirm)
        {
            btnConfirm.onClick.RemoveAllListeners();
            btnConfirm.onClick.AddListener(LoadMainMenu);
        }
        if (btnCancel)
        {
            btnCancel.onClick.RemoveAllListeners();
            btnCancel.onClick.AddListener(CloseConfirm);
        }

        // Make sure confirm panel starts hidden
        if (confirmPanel && confirmPanel.activeSelf)
        {
            Debug.Log("[ReturnToMainMenuButton] Confirm panel was active at start; hiding.");
            confirmPanel.SetActive(false);
        }
        if (confirmCanvasGroup)
        {
            confirmCanvasGroup.alpha = 0f;
            confirmCanvasGroup.interactable = false;
            confirmCanvasGroup.blocksRaycasts = false;
        }
    }

    private void OnPressed()
    {
        // Guard: another script might still be on this button
        // (e.g., LoadSceneButton). Remove that script from the same GameObject.
        // This log helps diagnose double-handling.
        Debug.Log("[ReturnToMainMenuButton] Click detected.");

        if (askBeforeExit && confirmPanel != null)
        {
            OpenConfirm();
        }
        else
        {
            LoadMainMenu();
        }
    }

    private void OpenConfirm()
    {
        // Ensure the panel’s parents are active
        if (!confirmPanel.activeSelf)
            confirmPanel.SetActive(true);

        if (confirmCanvasGroup)
        {
            // fade in
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(Fade(confirmCanvasGroup, 1f, fadeDuration, setInteractable: true));
        }
        else
        {
            // no fade—just ensure visible & clickable
            var cg = confirmPanel.GetComponent<CanvasGroup>();
            if (cg)
            {
                cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true;
            }
        }
    }

    public void CloseConfirm()
    {
        if (confirmPanel == null) return;

        if (confirmCanvasGroup)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(Fade(confirmCanvasGroup, 0f, fadeDuration, setInteractable: false, onEnd: () =>
            {
                confirmPanel.SetActive(false);
            }));
        }
        else
        {
            var cg = confirmPanel.GetComponent<CanvasGroup>();
            if (cg)
            {
                cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;
            }
            confirmPanel.SetActive(false);
        }
    }

    public void LoadMainMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.LogError($"[ReturnToMainMenuButton] Scene '{mainMenuSceneName}' not found in Build Settings.");
            return;
        }
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator Fade(CanvasGroup cg, float target, float dur, bool setInteractable, System.Action onEnd = null)
    {
        if (!cg) yield break;
        float start = cg.alpha;
        float t = 0f;

        // set clickability up-front when showing, at end when hiding
        if (target > start && setInteractable)
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        cg.alpha = target;

        if (target <= 0f)
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        onEnd?.Invoke();
        _fadeRoutine = null;
    }
}
