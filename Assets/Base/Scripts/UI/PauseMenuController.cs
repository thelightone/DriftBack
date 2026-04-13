using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PauseMenuController : MonoBehaviour
{
    const string DefaultHomeSceneName = "Scenes/InitScene";

    static PauseMenuController _instance;

    [Header("Scene Navigation")]
    [SerializeField] private string homeSceneName = DefaultHomeSceneName;
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button homeButton;

    public static bool IsPaused { get; private set; }
    public static bool IsPauseMenuVisible =>
        _instance != null && _instance.pausePanel != null && _instance.pausePanel.activeInHierarchy;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        BindButtons();
        SetPaused(false);
    }

    void Update()
    {
        bool canPause = RaceFlowManager.Instance != null && !RaceFlowManager.Instance.IsRaceFinished;

        if (pauseButton != null)
            pauseButton.gameObject.SetActive(canPause);

        if (pausePanel != null && pausePanel.activeInHierarchy != IsPaused)
        {
            SetPaused(pausePanel.activeInHierarchy);
            return;
        }

        if (!canPause)
        {
            if (IsPaused)
                SetPaused(false);

            return;
        }

        if (Input.GetKeyDown(toggleKey))
            TogglePause();
    }

    void OnDestroy()
    {
        if (_instance != this)
            return;

        UnbindButtons();
        _instance = null;
        IsPaused = false;
        Time.timeScale = 1f;
    }

    public void TogglePause()
    {
        if (RaceFlowManager.Instance == null || RaceFlowManager.Instance.IsRaceFinished)
            return;

        SetPaused(!IsPaused);
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    public void RestartRace()
    {
        SetPaused(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoHome()
    {
        SetPaused(false);
        string targetScene = string.IsNullOrWhiteSpace(homeSceneName) ? DefaultHomeSceneName : homeSceneName;
        SceneManager.LoadScene(targetScene);
    }

    void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (pausePanel != null)
            pausePanel.SetActive(paused);
    }

    void BindButtons()
    {
        AddButtonListener(pauseButton, TogglePause);
        AddButtonListener(continueButton, ResumeGame);
        AddButtonListener(restartButton, RestartRace);
        AddButtonListener(homeButton, GoHome);
    }

    void UnbindButtons()
    {
        RemoveButtonListener(pauseButton, TogglePause);
        RemoveButtonListener(continueButton, ResumeGame);
        RemoveButtonListener(restartButton, RestartRace);
        RemoveButtonListener(homeButton, GoHome);
    }

    static void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }
}
