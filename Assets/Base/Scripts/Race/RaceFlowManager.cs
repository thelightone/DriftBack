using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Таймер заезда, финиш, очки. UI (Canvas, таймер, панель Race Over) собирается в редакторе и назначается полями ниже.
/// </summary>
[DefaultExecutionOrder(-40)]
public class RaceFlowManager : MonoBehaviour
{
    public static RaceFlowManager Instance { get; private set; }

    public static bool InputBlocked =>
        Instance != null && (!Instance._raceStarted || Instance._raceFinished || PauseMenuController.IsPaused);

    [Header("Gameplay")]
    [SerializeField] private int startCountdownSeconds = 3;
    [SerializeField] private float scoreTimeDivisor = 10000f;
    [SerializeField] private int minimumScore = 50;
    [SerializeField] private float minSpeedToCountTime = 0.6f;
    [Tooltip("Финиш засчитывается только после того, как игрок уедет от финишной линии на эту дистанцию.")]
    [SerializeField] private float requiredDistanceFromFinish = 250f;
    [Tooltip("Опционально. Если не назначено, будет найден первый FinishLineTrigger на сцене.")]
    [SerializeField] private Transform finishReferencePoint;

    [Header("UI — назначьте объекты с Canvas вручную")]
    [Tooltip("TextMeshPro — отображение времени заезда")]
    [SerializeField] private TextMeshProUGUI timerText;
    [Tooltip("Отдельный центральный текст для предстартового отсчета. Если не назначен, создается автоматически.")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [Tooltip("TextMeshPro — скорость машины (например 120 КМ/Ч). Опционально.")]
    [SerializeField] private TextMeshProUGUI speedText;
    [Tooltip("Корень панели (обычно с Image). Должна быть выключена в сцене до финиша.")]
    [SerializeField] private GameObject raceOverPanel;
    [Tooltip("Опционально. Если пусто — строка заголовка не меняется.")]
    [SerializeField] private TextMeshProUGUI raceOverTitleText;
    [SerializeField] private TextMeshProUGUI raceOverTimeText;
    [SerializeField] private TextMeshProUGUI raceOverScoreText;

    float _elapsedActiveDriving;
    bool _raceStarted;
    bool _raceFinished;
    int _finalScore;
    bool _warnedMissingTimer;
    bool _warnedMissingFinishReference;
    bool _finishUnlocked;
    bool _countdownTextCreatedAtRuntime;
    float _maxDistanceFromFinishSqr;

    public float ElapsedSeconds => _elapsedActiveDriving;
    public bool IsRaceStarted => _raceStarted && !_raceFinished;
    public bool IsRaceFinished => _raceFinished;
    public int FinalScore => _finalScore;

    public static event Action<float, int> RaceFinished;

    void Awake()
    {
        Instance = this;
        // OnPlayerFinished ставит паузу (timeScale = 0); при LoadScene значение не сбрасывается.
        Time.timeScale = 1f;

        if (finishReferencePoint == null)
        {
            var finishTrigger = FindObjectOfType<FinishLineTrigger>();
            if (finishTrigger != null)
                finishReferencePoint = finishTrigger.transform;
        }

        if (raceOverPanel != null)
            raceOverPanel.SetActive(false);
    }

    void Start()
    {
        StartCoroutine(BeginRaceCountdown());
    }

    void OnDestroy()
    {
        if (_countdownTextCreatedAtRuntime && countdownText != null)
            Destroy(countdownText.gameObject);

        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (_raceFinished || GameController.Instance == null)
            return;

        var car = GameController.PlayerCar;
        if (car == null)
            return;

        if (!_raceStarted)
        {
            if (speedText != null)
                speedText.text = $"{Mathf.RoundToInt(car.SpeedInHour)} КМ/Ч";
            return;
        }

        UpdateFinishAvailability(car);

        if (car.CurrentSpeed > minSpeedToCountTime)
            _elapsedActiveDriving += Time.deltaTime;

        if (timerText != null)
            timerText.text = FormatTime(_elapsedActiveDriving);
        else if (!_warnedMissingTimer)
        {
            _warnedMissingTimer = true;
            Debug.LogWarning(
                $"{nameof(RaceFlowManager)}: не назначен {nameof(timerText)} — таймер на экране не обновляется.",
                this);
        }

        if (speedText != null)
            speedText.text = $"{Mathf.RoundToInt(car.SpeedInHour)} КМ/Ч";
    }

    public void OnPlayerFinished(CarController car)
    {
        if (!_raceStarted || _raceFinished || car == null || GameController.PlayerCar == null)
            return;

        if (car != GameController.PlayerCar)
            return;

        if (!CanFinishRace())
            return;

        _raceFinished = true;
        _finalScore = ComputeScore(_elapsedActiveDriving);
        Time.timeScale = 0f;

        var uc = car.GetComponent<UserControl>();
        if (uc != null)
            uc.enabled = false;

        ShowRaceOverUi();
        RaceFinished?.Invoke(_elapsedActiveDriving, _finalScore);
    }

    IEnumerator BeginRaceCountdown()
    {
        _raceStarted = false;
        _elapsedActiveDriving = 0f;
        SetTimerText(FormatTime(0f));
        ShowCountdownText(false, string.Empty);

        int countdown = Mathf.Max(0, startCountdownSeconds);
        for (int value = countdown; value > 0; value--)
        {
            ShowCountdownText(true, value.ToString());
            yield return new WaitForSeconds(1f);
        }

        ShowCountdownText(false, string.Empty);
        _raceStarted = true;
        SetTimerText(FormatTime(0f));
    }

    bool CanFinishRace()
    {
        if (requiredDistanceFromFinish <= 0f)
            return true;

        if (_finishUnlocked)
            return true;

        if (finishReferencePoint == null)
        {
            if (!_warnedMissingFinishReference)
            {
                _warnedMissingFinishReference = true;
                Debug.LogWarning(
                    $"{nameof(RaceFlowManager)}: не назначен {nameof(finishReferencePoint)} и не найден {nameof(FinishLineTrigger)}. Проверка полного прохождения трассы отключена.",
                    this);
            }

            return true;
        }

        return false;
    }

    void UpdateFinishAvailability(CarController car)
    {
        if (_finishUnlocked || requiredDistanceFromFinish <= 0f || car == null)
            return;

        if (finishReferencePoint == null)
            return;

        float currentDistanceSqr = (car.transform.position - finishReferencePoint.position).sqrMagnitude;
        if (currentDistanceSqr > _maxDistanceFromFinishSqr)
            _maxDistanceFromFinishSqr = currentDistanceSqr;

        float requiredDistanceSqr = requiredDistanceFromFinish * requiredDistanceFromFinish;
        if (_maxDistanceFromFinishSqr >= requiredDistanceSqr)
            _finishUnlocked = true;
    }

    int ComputeScore(float seconds)
    {
        float t = Mathf.Max(seconds, 0.05f);
        return Mathf.Max(minimumScore, Mathf.RoundToInt(scoreTimeDivisor / t));
    }

    static string FormatTime(float seconds)
    {
        var t = TimeSpan.FromSeconds(seconds);
        return t.Minutes > 0
            ? $"{(int)t.TotalMinutes:00}:{t.Seconds:00}.{t.Milliseconds / 10:00}"
            : $"{t.Seconds:00}.{t.Milliseconds / 10:00}";
    }

    void SetTimerText(string value)
    {
        if (timerText != null)
            timerText.text = value;
    }

    void ShowCountdownText(bool visible, string value)
    {
        var targetText = GetOrCreateCountdownText();
        if (targetText == null)
            return;

        targetText.gameObject.SetActive(visible);
        if (visible)
            targetText.text = value;
    }

    TextMeshProUGUI GetOrCreateCountdownText()
    {
        if (countdownText != null)
            return countdownText;

        Canvas parentCanvas = timerText != null ? timerText.GetComponentInParent<Canvas>() : FindObjectOfType<Canvas>();
        if (parentCanvas == null)
        {
            var canvasObject = new GameObject("CountdownCanvas");
            parentCanvas = canvasObject.AddComponent<Canvas>();
            parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        var countdownObject = new GameObject("CountdownText");
        countdownObject.transform.SetParent(parentCanvas.transform, false);

        var rectTransform = countdownObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        countdownText = countdownObject.AddComponent<TextMeshProUGUI>();
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.fontSize = 160f;
        countdownText.fontStyle = FontStyles.Bold;
        countdownText.color = Color.white;
        countdownText.raycastTarget = false;
        countdownText.text = string.Empty;
        countdownObject.SetActive(false);

        _countdownTextCreatedAtRuntime = true;
        return countdownText;
    }

    void ShowRaceOverUi()
    {
        if (raceOverPanel == null)
        {
            Debug.LogWarning(
                $"{nameof(RaceFlowManager)}: не назначен {nameof(raceOverPanel)} — окно Race Over не показано.",
                this);
            return;
        }

        if (raceOverTitleText != null)
            raceOverTitleText.text = "Race Over";

        if (raceOverTimeText != null)
            raceOverTimeText.text = FormatTime(_elapsedActiveDriving);

        if (raceOverScoreText != null)
            raceOverScoreText.text = _finalScore.ToString();

        raceOverPanel.SetActive(true);
    }
}
