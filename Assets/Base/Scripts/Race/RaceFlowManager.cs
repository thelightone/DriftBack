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
        Instance != null &&
        Instance.isActiveAndEnabled &&
        (!Instance._raceStarted || Instance._raceFinished || PauseMenuController.IsPaused);

    [Header("Gameplay")]
    [SerializeField] private int startCountdownSeconds = 3;
    [SerializeField] private float minSpeedToCountTime = 0.6f;
    [SerializeField] private float scoreTimeDivisor = 10000f;
    [SerializeField] private int minimumScore = 50;
    [Header("Drift score")]
    [SerializeField] private float driftPointsPerSecond = 40f;
    [SerializeField] private float minDriftSpeedKmh = 20f;
    [SerializeField] private float minDriftSlip = 0.35f;
    [SerializeField] private float minDriftAngle = 12f;
    private const int MinDriftGainTextPoints = 10;
    [Tooltip("Финиш засчитывается только после того, как игрок уедет от финишной линии на эту дистанцию.")]
    [SerializeField] private float requiredDistanceFromFinish = 250f;
    [Tooltip("Опционально. Если не назначено, будет найден первый FinishLineTrigger на сцене.")]
    [SerializeField] private Transform finishReferencePoint;
    [Header("Countdown animation")]
    [SerializeField] private float countdownScaleFrom = 0.75f;
    [SerializeField] private float countdownScaleTo = 1.35f;
    [SerializeField] private float countdownFadeOutStart = 0.25f;

    [Header("UI — назначьте объекты с Canvas вручную")]
    [Tooltip("TextMeshPro — отображение времени заезда")]
    [SerializeField] private TextMeshProUGUI timerText;
    [Tooltip("Отдельный центральный текст для предстартового отсчета. Если не назначен, создается автоматически.")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [Tooltip("TextMeshPro — скорость машины (например 120). Опционально.")]
    [SerializeField] private TextMeshProUGUI speedText;
    [Tooltip("Насколько быстро UI-скорость догоняет реальную (больше = резче, меньше = плавнее).")]
    [SerializeField] private float speedTextSmoothing = 10f;
    [Tooltip("TextMeshPro — очки дрифта. Если не назначен, создается автоматически.")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [Tooltip("TextMeshPro — всплывающий текст +X очков во время активного дрифта. Объект будет включаться/выключаться автоматически.")]
    [SerializeField] private TextMeshProUGUI driftGainText;
    [Tooltip("Корень панели (обычно с Image). Должна быть выключена в сцене до финиша.")]
    [SerializeField] private GameObject raceOverPanel;
    [Tooltip("Опционально. Если пусто — строка заголовка не меняется.")]
    [SerializeField] private TextMeshProUGUI raceOverTitleText;
    [SerializeField] private TextMeshProUGUI raceOverTimeText;
    [SerializeField] private TextMeshProUGUI raceOverDriftScoreText;
    [SerializeField] private TextMeshProUGUI raceOverTimeBonusText;
    [SerializeField] private TextMeshProUGUI raceOverScoreText;

    float _elapsedActiveDriving;
    bool _raceStarted;
    bool _raceFinished;
    int _finalScore;
    bool _warnedMissingTimer;
    bool _warnedMissingFinishReference;
    bool _finishUnlocked;
    bool _countdownTextCreatedAtRuntime;
    bool _scoreTextCreatedAtRuntime;
    float _maxDistanceFromFinishSqr;
    bool _isInDrift;
    float _pendingDriftPoints;
    int _accumulatedDriftScore;
    int _finalTimeScore;
    float _smoothedSpeedDisplay;
    bool _isSpeedDisplayInitialized;

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

        if (_scoreTextCreatedAtRuntime && scoreText != null)
            Destroy(scoreText.gameObject);

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
            UpdateSpeedText(car);
            return;
        }

        UpdateFinishAvailability(car);

        if (car.CurrentSpeed > minSpeedToCountTime)
            _elapsedActiveDriving += Time.deltaTime;

        UpdateDriftScoring(car);

        if (timerText != null)
            timerText.text = FormatTime(_elapsedActiveDriving);
        else if (!_warnedMissingTimer)
        {
            _warnedMissingTimer = true;
            Debug.LogWarning(
                $"{nameof(RaceFlowManager)}: не назначен {nameof(timerText)} — таймер на экране не обновляется.",
                this);
        }

        UpdateSpeedText(car);
    }

    public void OnPlayerFinished(CarController car)
    {
        if (!_raceStarted || _raceFinished || car == null || GameController.PlayerCar == null)
            return;

        if (car != GameController.PlayerCar)
            return;

        if (!CanFinishRace())
            return;

        CommitPendingDriftPoints();
        _raceFinished = true;
        _finalTimeScore = ComputeTimeScore(_elapsedActiveDriving);
        _finalScore = _accumulatedDriftScore + _finalTimeScore;
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
        _isInDrift = false;
        _isSpeedDisplayInitialized = false;
        _pendingDriftPoints = 0f;
        _accumulatedDriftScore = 0;
        _finalTimeScore = 0;
        SetTimerText(FormatTime(0f));
        SetScoreText(0);
        SetDriftGainText(false, 0);
        ShowCountdownText(false, string.Empty);

        int countdown = Mathf.Max(0, startCountdownSeconds);
        for (int value = countdown; value > 0; value--)
        {
            yield return AnimateCountdownValue(value.ToString(), 1f);
        }
        yield return AnimateCountdownValue("СТАРТ", 0.9f);

        ShowCountdownText(false, string.Empty);
        _raceStarted = true;
        SetTimerText(FormatTime(0f));
    }

    IEnumerator AnimateCountdownValue(string value, float duration)
    {
        var targetText = GetOrCreateCountdownText();
        if (targetText == null)
            yield break;

        float safeDuration = Mathf.Max(0.01f, duration);
        float fadeStart = Mathf.Clamp01(countdownFadeOutStart);
        float fromScale = Mathf.Max(0.01f, countdownScaleFrom);
        float toScale = Mathf.Max(fromScale, countdownScaleTo);

        targetText.text = value;
        targetText.gameObject.SetActive(true);
        targetText.rectTransform.localScale = Vector3.one * fromScale;

        Color baseColor = targetText.color;
        baseColor.a = 1f;
        targetText.color = baseColor;

        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / safeDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            float scale = Mathf.LerpUnclamped(fromScale, toScale, easedProgress);
            targetText.rectTransform.localScale = Vector3.one * scale;

            float alpha = progress <= fadeStart
                ? 1f
                : Mathf.Lerp(1f, 0f, Mathf.InverseLerp(fadeStart, 1f, progress));

            var animatedColor = baseColor;
            animatedColor.a = alpha;
            targetText.color = animatedColor;

            yield return null;
        }

        var hiddenColor = baseColor;
        hiddenColor.a = 0f;
        targetText.color = hiddenColor;
        targetText.rectTransform.localScale = Vector3.one;
        targetText.gameObject.SetActive(false);
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

    void UpdateDriftScoring(CarController car)
    {
        bool isDriftingNow = IsCarDrifting(car);
        if (isDriftingNow)
        {
            _pendingDriftPoints += Mathf.Max(0f, driftPointsPerSecond) * Time.deltaTime;
            int displayedPoints = Mathf.Max(1, Mathf.RoundToInt(_pendingDriftPoints));
            SetDriftGainText(true, displayedPoints);
        }
        else if (_isInDrift)
        {
            CommitPendingDriftPoints();
            SetDriftGainText(false, 0);
        }

        _isInDrift = isDriftingNow;
    }

    bool IsCarDrifting(CarController car)
    {
        if (car == null)
            return false;

        if (car.SpeedInHour < minDriftSpeedKmh)
            return false;

        if (Mathf.Abs(car.VelocityAngle) < minDriftAngle)
            return false;

        return car.CurrentMaxSlip >= minDriftSlip;
    }

    void CommitPendingDriftPoints()
    {
        int pointsToAdd = Mathf.RoundToInt(_pendingDriftPoints);
        if (pointsToAdd <= 0)
        {
            _pendingDriftPoints = 0f;
            SetDriftGainText(false, 0);
            return;
        }

        _accumulatedDriftScore += pointsToAdd;
        _pendingDriftPoints = 0f;
        SetScoreText(_accumulatedDriftScore);
        SetDriftGainText(false, 0);
    }

    int ComputeTimeScore(float seconds)
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

    void SetScoreText(int score)
    {
        var targetText = GetOrCreateScoreText();
        if (targetText != null)
            targetText.text = score.ToString();
    }

    void SetDriftGainText(bool visible, int points)
    {
        if (driftGainText == null)
            return;

        bool shouldShow = visible && points >= MinDriftGainTextPoints;
        driftGainText.gameObject.SetActive(shouldShow);
        if (shouldShow)
            driftGainText.text = $"+{Mathf.Max(0, points)}";
    }

    void UpdateSpeedText(CarController car)
    {
        if (speedText == null || car == null)
            return;

        float targetSpeed = Mathf.Max(0f, car.SpeedInHour);
        if (!_isSpeedDisplayInitialized)
        {
            _smoothedSpeedDisplay = targetSpeed;
            _isSpeedDisplayInitialized = true;
        }
        else
        {
            float smoothFactor = 1f - Mathf.Exp(-Mathf.Max(0f, speedTextSmoothing) * Time.unscaledDeltaTime);
            _smoothedSpeedDisplay = Mathf.Lerp(_smoothedSpeedDisplay, targetSpeed, smoothFactor);
        }

        speedText.text = Mathf.RoundToInt(_smoothedSpeedDisplay).ToString();
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

    TextMeshProUGUI GetOrCreateScoreText()
    {
        if (scoreText != null)
            return scoreText;

        Canvas parentCanvas = timerText != null ? timerText.GetComponentInParent<Canvas>() : FindObjectOfType<Canvas>();
        if (parentCanvas == null)
            return null;

        var scoreObject = new GameObject("ScoreText");
        scoreObject.transform.SetParent(parentCanvas.transform, false);

        var rectTransform = scoreObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(40f, -40f);
        rectTransform.sizeDelta = new Vector2(420f, 70f);

        scoreText = scoreObject.AddComponent<TextMeshProUGUI>();
        scoreText.alignment = TextAlignmentOptions.Left;
        scoreText.fontSize = 42f;
        scoreText.fontStyle = FontStyles.Bold;
        scoreText.color = Color.white;
        scoreText.raycastTarget = false;
        scoreText.text = "0";

        _scoreTextCreatedAtRuntime = true;
        return scoreText;
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

        var driftScoreText = GetRaceOverText(
            raceOverDriftScoreText,
            "drift",
            "driftscore",
            "score_drift",
            "score drift");
        if (driftScoreText != null)
            driftScoreText.text = $"Drift: {_accumulatedDriftScore}";

        var timeBonusText = GetRaceOverText(
            raceOverTimeBonusText,
            "timebonus",
            "time bonus",
            "bonus",
            "score_time");
        if (timeBonusText != null)
            timeBonusText.text = $"Time bonus: {_finalTimeScore}";

        if (raceOverScoreText != null)
            raceOverScoreText.text = $"Total: {_finalScore}";

        raceOverPanel.SetActive(true);
    }

    TextMeshProUGUI GetRaceOverText(TextMeshProUGUI existing, params string[] nameHints)
    {
        if (existing != null)
            return existing;

        if (raceOverPanel == null)
            return null;

        var texts = raceOverPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            var candidate = texts[i];
            if (candidate == null)
                continue;

            string normalizedName = candidate.name.ToLowerInvariant();
            for (int j = 0; j < nameHints.Length; j++)
            {
                if (normalizedName.Contains(nameHints[j]))
                    return candidate;
            }
        }

        return null;
    }
}
