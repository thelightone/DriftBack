using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class AppManager : MonoBehaviour
{
    [Header("Default Config")]
    [SerializeField] private string defaultCarId = "car1";
    [SerializeField] private int tournamentEntryPrice = 100;
    [SerializeField] private string tournamentSeasonId = "";
    [SerializeField] private bool useLocalDebugPurchases = true;

    [Header("Links")]
    [SerializeField] private MainScreenView view;
    [SerializeField] private GaragePanelView garagePanelView;
    [SerializeField] private BuyCurrencyPanelView buyCurrencyPanelView;
    [SerializeField] private TournamentPanelView tournamentPanelView;
    [SerializeField] private LeaderboardPanelView leaderboardPanelView;
    [SerializeField] private GarageCatalog garageCatalog;
    [SerializeField] private CurrencyPackCatalog currencyPackCatalog;
    [SerializeField] private SceneLoader sceneLoader;

    [Header("Backend")]
    [SerializeField] private string backendBaseUrl = "https://your-backend-url.com";

    private static readonly float[] PaidInvoiceGarageRetryDelays = { 2f, 3f, 5f, 8f, 12f };

    private AppState _state;
    private TelegramBridge _telegramBridge;
    private BackendApi _backendApi;
    private GarageResponse _lastGarageResponse;
    private Coroutine _invoiceRefreshCoroutine;
    private int _invoiceBalanceSnapshot;
    private string _tournamentHighScoreDisplay = "—";
    private string _mainPanelTournamentRecordDisplay = "—";
    private string _mainPanelTournamentPlaceDisplay = "—";
    private string _tournamentPanelRatingPlaceDisplay = "—";
    private string _tournamentPanelFirstPlaceScoreDisplay = "—";
    private string _tournamentPanelDateRangeDisplay = "—";
    private int _activeSeasonEntryFee = 0;

    private bool _showServerNickOnNickButton;
    private INickChangeUi _nickChangeUi;

    private void Awake()
    {
        Debug.Log("=== APP MANAGER AWAKE ===");
        Debug.Log("Backend URL: " + backendBaseUrl);

        _state = new AppState
        {
            OwnedCarIds = new[] { defaultCarId },
            IsPremium = false
        };

        _telegramBridge = new TelegramBridge();
        _backendApi = new BackendApi(backendBaseUrl);
    }

    private void Start()
    {
        Debug.Log("=== APP MANAGER START ===");

        CollectTelegramData();
        UpdateTelegramView();
        _telegramBridge.ReadyAndExpand();

        LoadCachedProfile();
        LoadSelectedCar();
        RebuildPanels();
        RefreshAllViews();

        StartCoroutine(InitFlow());
    }

    private void CollectTelegramData()
    {
        _state.TelegramAvailable = _telegramBridge.IsAvailable();
        _state.InitData = _telegramBridge.GetInitData();
        _state.TelegramUser = _telegramBridge.GetUser();
        _state.StartParam = _telegramBridge.GetStartParam();
        _state.Platform = _telegramBridge.GetPlatform();
        _state.AppVersion = _telegramBridge.GetVersion();

        Debug.Log("=== TELEGRAM DATA ===");
        Debug.Log("Telegram available: " + _state.TelegramAvailable);
        Debug.Log("Telegram initData: " + (_state.InitData ?? ""));
        Debug.Log("Telegram user id: " + (_state.TelegramUser != null ? _state.TelegramUser.id.ToString() : "null"));
        Debug.Log("Telegram platform: " + _state.Platform);
        Debug.Log("Telegram appVersion: " + _state.AppVersion);
    }

    private void UpdateTelegramView()
    {
        if (view != null)
            view.ShowTelegramData(_state.TelegramAvailable, _state.TelegramUser, _state.InitData);
    }

    private void LoadCachedProfile()
    {
        var cached = LocalProfileCache.Load();

        _state.PlayerId = cached.playerId;
        _state.PlayerNick = string.Empty;
        _state.OwnedCarIds = cached.ownedCarIds ?? Array.Empty<string>();
        _state.IsPremium = false;
        _state.TrainingPoints = cached.trainingPoints;
        _state.TournamentPoints = cached.tournamentPoints;
        _state.SoftCurrency = 0;
        _state.AccessToken = string.Empty;
        _state.GarageRevision = 0;

        Debug.Log("=== CACHED PROFILE LOADED ===");
        Debug.Log("Cached PlayerId: " + _state.PlayerId);
        Debug.Log("Cached OwnedCarIds: " + (_state.OwnedCarIds != null ? string.Join(",", _state.OwnedCarIds) : "NULL"));
        Debug.Log("Cached TrainingPoints: " + _state.TrainingPoints);
        Debug.Log("Cached TournamentPoints: " + _state.TournamentPoints);
    }

    private void LoadSelectedCar()
    {
        _state.SelectedCarId = SelectedCarStorage.Load();

        if (string.IsNullOrWhiteSpace(_state.SelectedCarId))
        {
            _state.SelectedCarId = defaultCarId;
            SelectedCarStorage.Save(defaultCarId);
        }

        Debug.Log("SelectedCarId after load: " + _state.SelectedCarId);
    }

    private void SaveProfileCache()
    {
        LocalProfileCache.Save(
            _state.PlayerId,
            _state.OwnedCarIds,
            _state.TrainingPoints,
            _state.TournamentPoints,
            _state.SoftCurrency
        );

        Debug.Log("Profile cache saved. PlayerId=" + _state.PlayerId +
                  ", OwnedCars=" + (_state.OwnedCarIds != null ? string.Join(",", _state.OwnedCarIds) : "NULL"));
    }

    private void RefreshAllViews(string error = "")
    {
        if (view != null)
        {
            view.ShowProfile(
                _state.IsAuthorized,
                ResolveMainProfileDisplayName(),
                _state.OwnedCarIds,
                _state.IsPremium,
                _state.TrainingPoints,
                _state.TournamentPoints,
                _mainPanelTournamentRecordDisplay,
                _mainPanelTournamentPlaceDisplay,
                _state.SoftCurrency,
                _state.SelectedCarId,
                error,
                GetSelectedCarIcon()
            );
        }

        _nickChangeUi?.RefreshButtonLabel();
    }

    public void RegisterNickChangeUi(INickChangeUi ui)
    {
        _nickChangeUi = ui;
    }

    public string GetNickButtonLabelText()
    {
        if (_showServerNickOnNickButton && !string.IsNullOrWhiteSpace(_state.PlayerNick))
            return _state.PlayerNick.Trim();
        return FormatTelegramUsernameForButton(_state.TelegramUser);
    }

    public string GetNickForEditField()
    {
        return string.IsNullOrWhiteSpace(_state.PlayerNick) ? string.Empty : _state.PlayerNick.Trim();
    }

    private static string FormatTelegramUsernameForButton(TelegramUserData u)
    {
        if (u == null)
            return "Ник";
        if (!string.IsNullOrWhiteSpace(u.username))
            return "@" + u.username.Trim();
        if (!string.IsNullOrWhiteSpace(u.first_name))
            return u.first_name.Trim();
        if (u.id > 0)
            return "id " + u.id;
        return "Ник";
    }

    private Sprite GetSelectedCarIcon()
    {
        if (garageCatalog == null || string.IsNullOrWhiteSpace(_state.SelectedCarId))
            return null;

        var def = garageCatalog.GetById(_state.SelectedCarId);
        return def != null ? def.icon : null;
    }

    private string ResolveMainProfileDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(_state.PlayerNick))
            return _state.PlayerNick;

        return _state.PlayerId;
    }

    private void RebuildPanels()
    {
        Debug.Log("=== REBUILD PANELS ===");
        Debug.Log("garageCatalog: " + (garageCatalog != null ? garageCatalog.name : "NULL"));
        Debug.Log("currencyPackCatalog: " + (currencyPackCatalog != null ? currencyPackCatalog.name : "NULL"));
        Debug.Log("SoftCurrency: " + _state.SoftCurrency);
        Debug.Log("OwnedCarIds: " + (_state.OwnedCarIds != null ? string.Join(",", _state.OwnedCarIds) : "NULL"));

        if (garagePanelView != null)
        {
            garagePanelView.Rebuild(
                garageCatalog,
                _state.OwnedCarIds,
                _state.SelectedCarId,
                _state.SoftCurrency,
                OnGarageCarAction
            );
        }
        else
        {
            Debug.LogError("garagePanelView is NULL");
        }

        if (buyCurrencyPanelView != null)
        {
            buyCurrencyPanelView.Rebuild(
                currencyPackCatalog,
                OnCurrencyPackSelected
            );
        }
        else
        {
            Debug.LogError("buyCurrencyPanelView is NULL");
        }

        RebuildTournamentPanel();
    }

    private int ActiveSeasonEntryFee()
    {
        return _activeSeasonEntryFee > 0 ? _activeSeasonEntryFee : tournamentEntryPrice;
    }

    private void RebuildTournamentPanel()
    {
        if (tournamentPanelView == null)
            return;

        tournamentPanelView.ShowData(
            _state.SoftCurrency,
            ActiveSeasonEntryFee(),
            _state.SelectedCarId,
            _state.IsPremium,
            _tournamentHighScoreDisplay,
            _tournamentPanelRatingPlaceDisplay,
            _tournamentPanelFirstPlaceScoreDisplay,
            _tournamentPanelDateRangeDisplay
        );
    }

    private void ResetMainPanelTournamentStats()
    {
        _mainPanelTournamentRecordDisplay = "—";
        _mainPanelTournamentPlaceDisplay = "—";
        _tournamentPanelRatingPlaceDisplay = "—";
        _tournamentPanelFirstPlaceScoreDisplay = "—";
        _tournamentPanelDateRangeDisplay = "—";
    }

    private static string FormatTournamentDateRange(string startsAt, string endsAt)
    {
        string start = FormatSingleSeasonDate(startsAt);
        string end = FormatSingleSeasonDate(endsAt);

        if (start == "—" && end == "—")
            return "—";
        if (start == "—")
            return end;
        if (end == "—")
            return start;
        return start + "-" + end;
    }

    private static string FormatSingleSeasonDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "—";

        value = value.Trim();

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return dt.ToString("dd.MM", CultureInfo.InvariantCulture);

        if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
            return dt.ToString("dd.MM", CultureInfo.CurrentCulture);

        return value;
    }

    private static string ResolveNickFromProfile(TelegramProfile profile)
    {
        if (profile == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(profile.nick))
            return profile.nick.Trim();
        if (!string.IsNullOrWhiteSpace(profile.username))
            return profile.username.Trim();
        if (!string.IsNullOrWhiteSpace(profile.firstName))
            return profile.firstName.Trim();

        return string.Empty;
    }

    private void ApplyLeaderboardResponseToRatingUi(LeaderboardResponse response, string leaderboardErr)
    {
        _mainPanelTournamentPlaceDisplay = "—";
        _tournamentPanelRatingPlaceDisplay = "—";
        _tournamentPanelFirstPlaceScoreDisplay = "—";

        if (!string.IsNullOrEmpty(leaderboardErr) || response == null)
            return;

        if (response.currentPlayer != null && response.currentPlayer.rank > 0)
        {
            string placeText = "#" + response.currentPlayer.rank;
            _mainPanelTournamentPlaceDisplay = placeText;
            _tournamentPanelRatingPlaceDisplay = placeText;
        }

        int leaderScore = -1;
        if (response.entries != null)
        {
            for (int i = 0; i < response.entries.Length; i++)
            {
                var entry = response.entries[i];
                if (entry != null && entry.rank == 1)
                {
                    leaderScore = entry.bestScore;
                    break;
                }
            }

            if (leaderScore < 0 && response.entries.Length > 0 && response.entries[0] != null)
                leaderScore = response.entries[0].bestScore;
        }

        if (leaderScore >= 0)
            _tournamentPanelFirstPlaceScoreDisplay = leaderScore.ToString();
    }

    private IEnumerator RefreshMainPanelTournamentStatsCoroutine()
    {
        if (!_state.IsAuthorized || string.IsNullOrWhiteSpace(_state.AccessToken))
        {
            ResetMainPanelTournamentStats();
            RefreshAllViews();
            yield break;
        }

        string seasonId = null;
        string resolveErr = null;
        yield return ResolveActiveSeasonIdForTournament(
            id => seasonId = id,
            e => resolveErr = e);

        if (!string.IsNullOrEmpty(resolveErr) || string.IsNullOrEmpty(seasonId))
        {
            ResetMainPanelTournamentStats();
            RefreshAllViews();
            yield break;
        }

        SeasonDetailDto detail = null;
        string detailErr = null;
        yield return _backendApi.GetSeasonDetail(
            _state.AccessToken,
            seasonId,
            d => detail = d,
            e => detailErr = e);

        if (!string.IsNullOrEmpty(detailErr) || detail == null)
        {
            ResetMainPanelTournamentStats();
            RefreshAllViews();
            yield break;
        }

        _mainPanelTournamentRecordDisplay = detail.entered ? detail.bestScore.ToString() : "—";
        _mainPanelTournamentPlaceDisplay = "—";
        _tournamentPanelDateRangeDisplay = FormatTournamentDateRange(detail.startsAt, detail.endsAt);

        LeaderboardResponse leaderboardResponse = null;
        string leaderboardErr = null;
        yield return _backendApi.GetSeasonLeaderboard(
            _state.AccessToken,
            seasonId,
            10,
            r => leaderboardResponse = r,
            e => leaderboardErr = e);

        ApplyLeaderboardResponseToRatingUi(leaderboardResponse, leaderboardErr);

        RefreshAllViews();
    }

    private void EnsureSelectedCarValid()
    {
        if (HasCar(_state.SelectedCarId))
            return;

        if (_state.OwnedCarIds != null && _state.OwnedCarIds.Length > 0)
        {
            _state.SelectedCarId = _state.OwnedCarIds[0];
            SelectedCarStorage.Save(_state.SelectedCarId);
            return;
        }

        _state.SelectedCarId = defaultCarId;
        SelectedCarStorage.Save(defaultCarId);
    }

    private void ApplyAuthResponse(TelegramAuthResponse response)
    {
        _state.IsAuthorized = !string.IsNullOrWhiteSpace(response.accessToken) && response.profile != null;
        _state.AccessToken = response.accessToken ?? string.Empty;

        if (response.profile != null)
        {
            _state.PlayerId = response.profile.userId;
            _state.PlayerNick = ResolveNickFromProfile(response.profile);
            _state.OwnedCarIds = response.profile.ownedCarIds ?? Array.Empty<string>();
            _state.GarageRevision = response.profile.garageRevision;
            _state.SoftCurrency = response.profile.raceCoinsBalance;
        }

        EnsureSelectedCarValid();

        Debug.Log("=== AUTH RESPONSE APPLIED ===");
        Debug.Log("IsAuthorized: " + _state.IsAuthorized);
        Debug.Log("PlayerId: " + _state.PlayerId);
        Debug.Log("PlayerNick: " + _state.PlayerNick);
        Debug.Log("AccessToken: " + _state.AccessToken);
        Debug.Log("GarageRevision: " + _state.GarageRevision);
        Debug.Log("RaceCoinsBalance after auth: " + _state.SoftCurrency);
        Debug.Log("OwnedCarIds after auth: " + (_state.OwnedCarIds != null ? string.Join(",", _state.OwnedCarIds) : "NULL"));
    }

    private void ApplyGarageResponse(GarageResponse response)
    {
        _lastGarageResponse = response;
        _state.GarageRevision = response.garageRevision;
        _state.SoftCurrency = response.raceCoinsBalance;

        var ownedIds = new List<string>();

        if (response.cars != null)
        {
            for (int i = 0; i < response.cars.Length; i++)
            {
                var car = response.cars[i];
                if (car != null && car.owned && !string.IsNullOrWhiteSpace(car.carId))
                    ownedIds.Add(car.carId);
            }
        }

        _state.OwnedCarIds = ownedIds.ToArray();
        EnsureSelectedCarValid();

        Debug.Log("=== GARAGE RESPONSE APPLIED ===");
        Debug.Log("GarageRevision: " + _state.GarageRevision);
        Debug.Log("RaceCoinsBalance after garage: " + _state.SoftCurrency);
        Debug.Log("OwnedCarIds after garage: " + (_state.OwnedCarIds != null ? string.Join(",", _state.OwnedCarIds) : "NULL"));
    }

    public void OnInitButtonClicked()
    {
        StartCoroutine(InitFlow());
    }

    public void OnRefreshButtonClicked()
    {
        StartCoroutine(RefreshFlow());
    }

    public void OnChangeNickRequested(string newNick, Action onSuccess = null, Action<string> onError = null)
    {
        StartCoroutine(ChangeNickFlow(newNick, onSuccess, onError));
    }

    public void OnOpenGarageClicked()
    {
        Debug.Log("OnOpenGarageClicked called");
        RebuildPanels();
        if (view != null)
            view.ShowGaragePanel();
        else
            Debug.LogError("view is NULL in OnOpenGarageClicked");
    }

    public void OnCloseGarageClicked()
    {
        if (view != null)
            view.ShowMainPanel();
    }

    public void OnOpenBuyCurrencyClicked()
    {
        Debug.Log("OnOpenBuyCurrencyClicked called");

        RebuildPanels();

        if (view == null)
        {
            Debug.LogError("view is NULL in OnOpenBuyCurrencyClicked");
            return;
        }

        view.ShowBuyCurrencyPanel();
    }

    public void OnCloseBuyCurrencyClicked()
    {
        if (view != null)
            view.ShowMainPanel();
    }

    public void OnOpenTournamentClicked()
    {
        Debug.Log("OnOpenTournamentClicked called");

        RebuildPanels();
        StartCoroutine(RefreshTournamentDataCoroutine());

        if (view != null)
            view.ShowTournamentPanel();
    }

    public void OnCloseTournamentClicked()
    {
        if (view != null)
            view.ShowMainPanel();
    }

    public void OnOpenLeaderboardClicked()
    {
        Debug.Log("OnOpenLeaderboardClicked called");

        if (view != null)
            view.ShowLeaderboardPanel();

        if (leaderboardPanelView == null)
        {
            if (view != null)
                view.ShowStatus("LeaderboardPanelView is not assigned");
            return;
        }

        StartCoroutine(RefreshLeaderboardCoroutine());
    }

    public void OnCloseLeaderboardClicked()
    {
        if (view != null)
            view.ShowMainPanel();
    }

    private static bool IsNickValid(string nick)
    {
        if (string.IsNullOrWhiteSpace(nick))
            return false;

        string trimmed = nick.Trim();
        if (trimmed.Length < 3 || trimmed.Length > 20)
            return false;

        for (int i = 0; i < trimmed.Length; i++)
        {
            char c = trimmed[i];
            if (!(char.IsLetterOrDigit(c) || c == '_'))
                return false;
        }

        return true;
    }

    private static string BuildNickErrorMessage(string errorCode)
    {
        switch (errorCode)
        {
            case "INVALID_NICK":
                return "Nick must be 3-20 chars, letters/digits/underscore only.";
            case "NICK_ALREADY_TAKEN":
                return "This nick is already taken.";
            case "INSUFFICIENT_BALANCE":
                return "Not enough race coins for nick change.";
            case "UNAUTHORIZED":
                return "Authorize first (init).";
            case "NOT_FOUND":
                return "Player profile not found.";
            default:
                return "Nick update failed: " + errorCode;
        }
    }

    private IEnumerator ChangeNickFlow(string rawNick, Action onSuccess, Action<string> onError)
    {
        if (!_state.IsAuthorized || string.IsNullOrWhiteSpace(_state.AccessToken))
        {
            const string msg = "Authorize first (init)";
            if (view != null)
                view.ShowStatus(msg);
            onError?.Invoke(msg);
            yield break;
        }

        string nextNick = (rawNick ?? string.Empty).Trim();
        if (!IsNickValid(nextNick))
        {
            const string msg = "Nick must be 3-20 chars, letters/digits/underscore only.";
            if (view != null)
                view.ShowStatus(msg);
            onError?.Invoke(msg);
            yield break;
        }

        if (string.Equals(_state.PlayerNick, nextNick, StringComparison.Ordinal))
        {
            const string msg = "Nick is already set.";
            if (view != null)
                view.ShowStatus(msg);
            onError?.Invoke(msg);
            yield break;
        }

        if (view != null)
            view.ShowStatus("Updating nick...");

        UpdateNickResponse response = null;
        string error = null;
        yield return _backendApi.UpdateProfileNick(
            _state.AccessToken,
            new UpdateNickRequest { nick = nextNick },
            r => response = r,
            e => error = e);

        if (!string.IsNullOrEmpty(error) || response == null)
        {
            string message = BuildNickErrorMessage(error ?? "failed");
            if (view != null)
                view.ShowStatus(message);
            onError?.Invoke(message);
            yield break;
        }

        _state.PlayerNick = response.nick;
        _state.SoftCurrency = response.raceCoinsBalance;
        _showServerNickOnNickButton = true;

        SaveProfileCache();
        RebuildPanels();
        RefreshAllViews();

        onSuccess?.Invoke();

        if (view != null)
            view.ShowStatus($"Nick updated to {response.nick}");
    }

    public void OnBuyTournamentAccessClicked()
    {
        if (!_state.IsAuthorized || string.IsNullOrWhiteSpace(_state.AccessToken))
        {
            NotifyTournamentFlow("Authorize first (init)");
            return;
        }

        if (_state.SoftCurrency < ActiveSeasonEntryFee())
        {
            if (view != null)
                view.ShowBuyCurrencyPanel();

            return;
        }

        NotifyTournamentFlow("");
        StartCoroutine(EnterSeasonFlow());
    }

    public void OnStartTrainingClicked()
    {
        if (sceneLoader == null)
        {
            if (view != null)
                view.ShowStatus("SceneLoader is not assigned");
            return;
        }

        if (string.IsNullOrWhiteSpace(_state.PlayerId))
        {
            if (view != null)
                view.ShowStatus("PlayerId is missing. Run init first.");
            return;
        }

        if (!_state.IsAuthorized || string.IsNullOrWhiteSpace(_state.AccessToken))
        {
            if (view != null)
                view.ShowStatus("Authorize first (init)");
            return;
        }

        Debug.Log("Starting training race. PlayerId=" + _state.PlayerId);

        StartCoroutine(StartTrainingRaceFlow());
    }

    public void OnStartTournamentClicked()
    {
        if (sceneLoader == null)
        {
            NotifyTournamentFlow("SceneLoader is not assigned");
            return;
        }

        if (!_state.IsAuthorized || string.IsNullOrWhiteSpace(_state.AccessToken))
        {
            NotifyTournamentFlow("Authorize first (init)");
            return;
        }

        NotifyTournamentFlow("");
        StartCoroutine(StartTournamentRaceFlow());
    }

    private string ConfiguredTournamentSeasonId()
    {
        if (tournamentSeasonId == null)
            return "";
        string s = tournamentSeasonId.Trim();
        if (s == "0")
            return "";
        return s;
    }

    private void NotifyTournamentFlow(string message)
    {
        Debug.LogWarning("Tournament: " + message);
        if (tournamentPanelView != null)
            tournamentPanelView.SetTournamentFlowMessage(message);
        if (view != null)
            view.ShowStatus(message);
    }

    private IEnumerator ResolveActiveSeasonIdForTournament(Action<string> onResolved, Action<string> onError)
    {
        string seasonId = ConfiguredTournamentSeasonId();

        if (!string.IsNullOrEmpty(seasonId))
        {
            onResolved?.Invoke(seasonId);
            yield break;
        }

        SeasonsListResponse listResponse = null;
        string seasonsErr = null;
        yield return _backendApi.GetSeasons(
            _state.AccessToken,
            r => listResponse = r,
            e => seasonsErr = e);

        if (!string.IsNullOrEmpty(seasonsErr) || listResponse == null)
        {
            onError?.Invoke(seasonsErr ?? "failed");
            yield break;
        }

        if (listResponse.seasons == null || listResponse.seasons.Length == 0)
        {
            onError?.Invoke("no seasons");
            yield break;
        }

        for (int i = 0; i < listResponse.seasons.Length; i++)
        {
            var s = listResponse.seasons[i];
            if (s != null && s.status == "active" && !string.IsNullOrWhiteSpace(s.seasonId))
            {
                onResolved?.Invoke(s.seasonId);
                yield break;
            }
        }

        onError?.Invoke("no active season");
    }

    private IEnumerator RefreshTournamentDataCoroutine()
    {
        if (tournamentPanelView == null)
            yield break;

        if (!_state.IsAuthorized || string.IsNullOrWhiteSpace(_state.AccessToken))
        {
            _tournamentHighScoreDisplay = "—";
            _activeSeasonEntryFee = 0;
            ResetMainPanelTournamentStats();
            RebuildTournamentPanel();
            RefreshAllViews();
            yield break;
        }

        _tournamentHighScoreDisplay = "…";
        _tournamentPanelRatingPlaceDisplay = "…";
        _tournamentPanelFirstPlaceScoreDisplay = "…";
        _tournamentPanelDateRangeDisplay = "…";
        RebuildTournamentPanel();

        string seasonId = null;
        string resolveErr = null;
        yield return ResolveActiveSeasonIdForTournament(
            id => seasonId = id,
            e => resolveErr = e);

        if (!string.IsNullOrEmpty(resolveErr) || string.IsNullOrEmpty(seasonId))
        {
            _tournamentHighScoreDisplay = "—";
            _activeSeasonEntryFee = 0;
            ResetMainPanelTournamentStats();
            RebuildTournamentPanel();
            RefreshAllViews();
            yield break;
        }

        SeasonDetailDto detail = null;
        string detailErr = null;
        yield return _backendApi.GetSeasonDetail(
            _state.AccessToken,
            seasonId,
            d => detail = d,
            e => detailErr = e);

        if (!string.IsNullOrEmpty(detailErr) || detail == null)
        {
            _tournamentHighScoreDisplay = "—";
            _activeSeasonEntryFee = 0;
            ResetMainPanelTournamentStats();
            RebuildTournamentPanel();
            RefreshAllViews();
            yield break;
        }

        _activeSeasonEntryFee = detail.entryFee > 0 ? detail.entryFee : tournamentEntryPrice;
        _state.IsPremium = detail.entered;
        _tournamentHighScoreDisplay = detail.entered ? detail.bestScore.ToString() : "—";
        _mainPanelTournamentRecordDisplay = _tournamentHighScoreDisplay;
        _mainPanelTournamentPlaceDisplay = "—";
        _tournamentPanelDateRangeDisplay = FormatTournamentDateRange(detail.startsAt, detail.endsAt);

        LeaderboardResponse leaderboardResponse = null;
        string leaderboardErr = null;
        yield return _backendApi.GetSeasonLeaderboard(
            _state.AccessToken,
            seasonId,
            10,
            r => leaderboardResponse = r,
            e => leaderboardErr = e);

        ApplyLeaderboardResponseToRatingUi(leaderboardResponse, leaderboardErr);

        Debug.Log($"Tournament data refreshed. entered={detail.entered}, entryFee={detail.entryFee}, bestScore={detail.bestScore}");

        RebuildTournamentPanel();
        RefreshAllViews();
    }

    private IEnumerator RefreshLeaderboardCoroutine()
    {
        if (leaderboardPanelView == null)
            yield break;

        if (!_state.IsAuthorized || string.IsNullOrWhiteSpace(_state.AccessToken))
        {
            leaderboardPanelView.ShowError("Authorize first (init)");
            yield break;
        }

        leaderboardPanelView.ShowLoading("Top players");

        string seasonId = null;
        string resolveErr = null;
        yield return ResolveActiveSeasonIdForTournament(
            id => seasonId = id,
            e => resolveErr = e);

        if (!string.IsNullOrEmpty(resolveErr) || string.IsNullOrEmpty(seasonId))
        {
            leaderboardPanelView.ShowError("Seasons: " + (resolveErr ?? "failed"));
            yield break;
        }

        LeaderboardResponse response = null;
        string leaderboardErr = null;
        yield return _backendApi.GetSeasonLeaderboard(
            _state.AccessToken,
            seasonId,
            10,
            r => response = r,
            e => leaderboardErr = e);

        if (!string.IsNullOrEmpty(leaderboardErr) || response == null)
        {
            leaderboardPanelView.ShowError("Leaderboard: " + (leaderboardErr ?? "failed"));
            yield break;
        }

        leaderboardPanelView.ShowEntries("Top 10", response.entries, response.currentPlayer);
    }

    private IEnumerator EnterSeasonFlow()
    {
        NotifyTournamentFlow("Entering season…");

        string seasonId = null;
        string resolveErr = null;
        yield return ResolveActiveSeasonIdForTournament(
            id => seasonId = id,
            e => resolveErr = e);

        if (!string.IsNullOrEmpty(resolveErr) || string.IsNullOrEmpty(seasonId))
        {
            NotifyTournamentFlow("Seasons: " + (resolveErr ?? "failed"));
            yield break;
        }

        EnterSeasonResponse enterResponse = null;
        string enterErr = null;
        yield return _backendApi.EnterSeason(
            _state.AccessToken,
            seasonId,
            r => enterResponse = r,
            e => enterErr = e);

        if (!string.IsNullOrEmpty(enterErr))
        {
            NotifyTournamentFlow("Enter season: " + enterErr);
            yield break;
        }

        if (enterResponse != null)
            _state.SoftCurrency = enterResponse.raceCoinsBalance;

        _state.IsPremium = true;

        SaveProfileCache();
        RebuildPanels();
        RefreshAllViews();
        yield return StartCoroutine(RefreshMainPanelTournamentStatsCoroutine());
        NotifyTournamentFlow("");
    }

    private IEnumerator StartTournamentRaceFlow()
    {
        Debug.Log("Starting tournament race flow. PlayerId=" + _state.PlayerId);

        string seasonId = null;
        string resolveErr = null;
        yield return ResolveActiveSeasonIdForTournament(
            id => seasonId = id,
            e => resolveErr = e);

        if (!string.IsNullOrEmpty(resolveErr) || string.IsNullOrEmpty(seasonId))
        {
            NotifyTournamentFlow("Seasons: " + (resolveErr ?? "failed"));
            yield break;
        }

        EnterSeasonResponse enterResponse = null;
        string enterErr = null;
        yield return _backendApi.EnterSeason(
            _state.AccessToken,
            seasonId,
            r => enterResponse = r,
            e => enterErr = e);

        if (!string.IsNullOrEmpty(enterErr))
        {
            NotifyTournamentFlow("Season enter: " + enterErr);
            yield break;
        }

        if (enterResponse != null)
            _state.SoftCurrency = enterResponse.raceCoinsBalance;

        _state.IsPremium = true;

        SeasonRaceStartResponse startResponse = null;
        string startErr = null;
        yield return _backendApi.StartSeasonRace(
            _state.AccessToken,
            seasonId,
            r => startResponse = r,
            e => startErr = e);

        if (!string.IsNullOrEmpty(startErr) || startResponse == null)
        {
            NotifyTournamentFlow("Race start: " + (startErr ?? "failed"));
            yield break;
        }

        RaceSessionContext.BeginTournamentRace(
            _state.AccessToken,
            seasonId,
            startResponse.raceId,
            startResponse.seed,
            _state.PlayerId,
            _state.InitData,
            _state.TelegramUser != null ? _state.TelegramUser.id : 0,
            backendBaseUrl);

        sceneLoader.StartTournamentGame();
    }

    private IEnumerator StartTrainingRaceFlow()
    {
        TrainingRaceStartResponse startResponse = null;
        string startErr = null;
        yield return _backendApi.StartTrainingRace(
            _state.AccessToken,
            r => startResponse = r,
            e => startErr = e);

        if (!string.IsNullOrEmpty(startErr) || startResponse == null ||
            string.IsNullOrWhiteSpace(startResponse.seasonId))
        {
            if (view != null)
                view.ShowStatus("Training race start: " + (startErr ?? "failed"));
            yield break;
        }

        RaceSessionContext.BeginTrainingRace(
            _state.AccessToken,
            startResponse.seasonId,
            startResponse.raceId,
            startResponse.seed,
            _state.PlayerId,
            _state.InitData,
            _state.TelegramUser != null ? _state.TelegramUser.id : 0,
            backendBaseUrl,
            startResponse.mapId);

        sceneLoader.StartTrainingGame();
    }

    public void OnAddCoinsClicked()
    {
        _state.SoftCurrency += 100;

        SaveProfileCache();
        RebuildPanels();
        RefreshAllViews();

        if (view != null)
            view.ShowStatus("+100 coins added (debug)");

    }

    public bool HasCar(string carId)
    {
        if (string.IsNullOrWhiteSpace(carId) || _state.OwnedCarIds == null)
            return false;

        for (int i = 0; i < _state.OwnedCarIds.Length; i++)
        {
            if (_state.OwnedCarIds[i] == carId)
                return true;
        }

        return false;
    }

    private GarageCarDto FindGarageCar(string carId)
    {
        if (_lastGarageResponse == null || _lastGarageResponse.cars == null || string.IsNullOrWhiteSpace(carId))
            return null;

        for (int i = 0; i < _lastGarageResponse.cars.Length; i++)
        {
            var car = _lastGarageResponse.cars[i];
            if (car != null && car.carId == carId)
                return car;
        }

        return null;
    }

    private void SelectCar(string carId)
    {
        if (!HasCar(carId))
        {
            if (view != null)
                view.ShowStatus("Car is not owned");
            return;
        }

        _state.SelectedCarId = carId;
        SelectedCarStorage.Save(carId);

        RebuildPanels();
        RefreshAllViews();

        if (view != null)
            view.ShowStatus("Selected car: " + carId);

    }

    private void OnGarageCarAction(CarDefinition car)
    {
        Debug.Log("OnGarageCarAction called: " + (car != null ? car.carId : "NULL"));

        if (car == null)
            return;

        if (HasCar(car.carId))
        {
            SelectCar(car.carId);
            return;
        }

        StartCoroutine(BuyCarFlow(car));
    }

    private void OnCurrencyPackSelected(CurrencyPackDefinition pack)
    {
        Debug.Log("OnCurrencyPackSelected called: " + (pack != null ? pack.productId : "NULL"));

        if (pack == null)
        {
            Debug.LogError("OnCurrencyPackSelected: pack is NULL");
            return;
        }

        if (!_state.IsAuthorized || string.IsNullOrWhiteSpace(_state.AccessToken))
        {
            Debug.LogError("OnCurrencyPackSelected: not authorized or access token is empty");
            if (view != null)
                view.ShowStatus("Not authorized. Run init first.");
            return;
        }

        StartCoroutine(BuyCoinsPackFlow(pack));
    }

    private IEnumerator InitFlow()
    {
        Debug.Log("=== INIT FLOW START ===");

        if (view != null)
            view.ShowStatus("Authorizing...");

        Debug.Log("InitData: " + (_state.InitData ?? "NULL"));

        if (string.IsNullOrWhiteSpace(_state.InitData))
        {
            Debug.LogError("InitFlow: initData is EMPTY");
            RebuildPanels();
            RefreshAllViews("Telegram initData is empty");
            yield break;
        }

        bool authDone = false;
        bool authSucceeded = false;

        var authRequest = new TelegramAuthRequest
        {
            initData = _state.InitData
        };

        Debug.Log("Sending AuthTelegram request...");

        yield return StartCoroutine(_backendApi.AuthTelegram(
            authRequest,
            response =>
            {
                Debug.Log("Auth SUCCESS");
                Debug.Log("AccessToken: " + (response.accessToken ?? "NULL"));

                if (response.profile != null)
                {
                    Debug.Log("Profile.userId: " + response.profile.userId);
                    Debug.Log("Profile.nick: " + response.profile.nick);
                    Debug.Log("Profile.ownedCarIds: " + (response.profile.ownedCarIds != null
                        ? string.Join(",", response.profile.ownedCarIds)
                        : "NULL"));
                    Debug.Log("Profile.garageRevision: " + response.profile.garageRevision);
                    Debug.Log("Profile.raceCoinsBalance: " + response.profile.raceCoinsBalance);
                }
                else
                {
                    Debug.LogError("Auth SUCCESS but profile is NULL");
                }

                ApplyAuthResponse(response);
                Debug.Log("Returned from ApplyAuthResponse");

                Debug.Log("State after auth:");
                Debug.Log("PlayerId: " + _state.PlayerId);
                Debug.Log("AccessToken: " + _state.AccessToken);
                Debug.Log("SoftCurrency: " + _state.SoftCurrency);

                authSucceeded = true;
                authDone = true;
            },
            error =>
            {
                Debug.LogError("Auth ERROR: " + error);
                RebuildPanels();
                RefreshAllViews(error);
                authDone = true;
            }
        ));

        while (!authDone)
            yield return null;

        if (!authSucceeded)
        {
            Debug.LogError("InitFlow stopped: auth failed");
            yield break;
        }

        Debug.Log("Auth completed -> loading garage...");

        yield return StartCoroutine(LoadGarageFlow());
        yield return StartCoroutine(RefreshMainPanelTournamentStatsCoroutine());

        SaveProfileCache();

        Debug.Log("=== INIT FLOW END ===");
    }

    private IEnumerator RefreshFlow()
    {
        Debug.Log("=== REFRESH FLOW START ===");

        if (view != null)
            view.ShowStatus("Refreshing profile...");

        if (!_state.IsAuthorized || string.IsNullOrWhiteSpace(_state.AccessToken))
        {
            Debug.LogWarning("RefreshFlow: no auth or token, running full InitFlow");
            yield return InitFlow();
            yield break;
        }

        yield return LoadGarageFlow();
        yield return StartCoroutine(RefreshMainPanelTournamentStatsCoroutine());

        SaveProfileCache();

        if (view != null)
            view.ShowStatus("Profile refreshed");
        Debug.Log("=== REFRESH FLOW END ===");
    }

    private IEnumerator LoadGarageFlow()
    {
        Debug.Log("=== LOAD GARAGE START ===");

        bool done = false;

        Debug.Log("AccessToken used: " + _state.AccessToken);

        yield return StartCoroutine(_backendApi.GetGarage(
            _state.AccessToken,
            response =>
            {
                Debug.Log("Garage SUCCESS");

                if (response.cars != null)
                {
                    Debug.Log("Garage cars count: " + response.cars.Length);
                    Debug.Log("Garage raceCoinsBalance: " + response.raceCoinsBalance);

                    for (int i = 0; i < response.cars.Length; i++)
                    {
                        var c = response.cars[i];
                        string priceText = "NULL";
                        string currencyText = "NULL";

                        if (c != null && c.price != null)
                        {
                            priceText = c.price.amount.ToString();
                            currencyText = c.price.currency;
                        }

                        Debug.Log($"Car[{i}]: id={c?.carId}, title={c?.title}, owned={c?.owned}, canBuy={c?.canBuy}, price={priceText}, currency={currencyText}");
                    }
                }
                else
                {
                    Debug.LogError("Garage response.cars is NULL");
                }

                ApplyGarageResponse(response);

                Debug.Log("OwnedCarIds after garage: " +
                          (_state.OwnedCarIds != null ? string.Join(",", _state.OwnedCarIds) : "NULL"));

                RebuildPanels();
                RefreshAllViews();

                done = true;
            },
            error =>
            {
                Debug.LogError("Garage ERROR: " + error);
                RebuildPanels();
                RefreshAllViews(error);
                done = true;
            }
        ));

        while (!done)
            yield return null;

        Debug.Log("=== LOAD GARAGE END ===");
    }

    private IEnumerator BuyCoinsPackFlow(CurrencyPackDefinition pack)
    {
        if (view != null)
            view.ShowStatus($"Creating invoice for {pack.displayName}...");

        Debug.Log("=== BUY COINS PACK FLOW START ===");
        Debug.Log("BundleId: " + pack.productId);
        Debug.Log("DisplayName: " + pack.displayName);
        Debug.Log("CoinsAmount(local config): " + pack.softCurrencyAmount);
        Debug.Log("AccessToken: " + _state.AccessToken);

        var request = new CreateCoinsPurchaseIntentRequest
        {
            bundleId = pack.productId
        };

        bool done = false;

        yield return StartCoroutine(_backendApi.CreateCoinsPurchaseIntent(
            _state.AccessToken,
            request,
            response =>
            {
                Debug.Log("CreateCoinsPurchaseIntent SUCCESS");
                Debug.Log("purchaseId: " + response.purchaseId);
                Debug.Log("status: " + response.status);
                Debug.Log("invoiceUrl: " + response.invoiceUrl);
                Debug.Log("expiresAt: " + response.expiresAt);
                Debug.Log("coinsAmount(from backend): " + response.coinsAmount);

                if (string.IsNullOrWhiteSpace(response.invoiceUrl))
                {
                    Debug.LogError("CreateCoinsPurchaseIntent SUCCESS but invoiceUrl is empty");

                    if (view != null)
                        view.ShowStatus("Coins purchase intent created, but invoiceUrl is empty");

                    done = true;
                    return;
                }

                if (view != null)
                    view.ShowStatus("Opening Stars invoice for coins bundle...");

                _invoiceBalanceSnapshot = _state.SoftCurrency;
                Debug.Log("Calling TelegramBridge.OpenInvoice for coins bundle...");
                _telegramBridge.OpenInvoice(response.invoiceUrl, gameObject.name, nameof(OnInvoiceClosed));

                if (_invoiceRefreshCoroutine != null)
                    StopCoroutine(_invoiceRefreshCoroutine);
                _invoiceRefreshCoroutine = StartCoroutine(PollGarageUntilBalanceChanges(_invoiceBalanceSnapshot));

                done = true;
            },
            error =>
            {
                Debug.LogError("Coins purchase intent ERROR: " + error);

                if (view != null)
                    view.ShowStatus("Coins purchase intent error: " + error);

                done = true;
            }
        ));

        while (!done)
            yield return null;

        Debug.Log("=== BUY COINS PACK FLOW END ===");
    }

    private IEnumerator BuyCarFlow(CarDefinition car)
    {
        if (!_state.IsAuthorized || string.IsNullOrWhiteSpace(_state.AccessToken))
        {
            if (view != null)
                view.ShowStatus("Not authorized. Run init first.");
            yield break;
        }

        if (HasCar(car.carId))
        {
            SelectCar(car.carId);
            yield break;
        }

        var garageCar = FindGarageCar(car.carId);
        int priceAmount;
        string priceCurrency;

        if (garageCar != null && garageCar.price != null)
        {
            priceAmount = garageCar.price.amount;
            priceCurrency = string.IsNullOrWhiteSpace(garageCar.price.currency)
                ? "RC"
                : garageCar.price.currency;
        }
        else
        {
            priceAmount = car.softCurrencyPrice;
            priceCurrency = "RC";
        }

        if (priceCurrency != "RC")
        {
            if (view != null)
                view.ShowStatus("Backend still returns non-RC car pricing. Live backend is probably outdated.");
            yield break;
        }

        if (_state.SoftCurrency < priceAmount)
        {
            if (view != null)
            {
                view.ShowStatus($"Not enough race coins for {car.displayName}. Opening Buy Currency...");
                RebuildPanels();
                view.ShowBuyCurrencyPanel();
            }
            yield break;
        }

        if (view != null)
            view.ShowStatus($"Buying {car.displayName} for race coins...");

        Debug.Log("=== BUY CAR FLOW START ===");
        Debug.Log("CarId: " + car.carId);
        Debug.Log("AccessToken: " + _state.AccessToken);
        Debug.Log("Current RaceCoinsBalance: " + _state.SoftCurrency);

        var request = new BuyCarRequest
        {
            carId = car.carId
        };

        bool done = false;

        yield return StartCoroutine(_backendApi.BuyCarWithRaceCoins(
            _state.AccessToken,
            request,
            response =>
            {
                Debug.Log("BuyCarWithRaceCoins SUCCESS");
                Debug.Log("success: " + response.success);
                Debug.Log("carId: " + response.carId);
                Debug.Log("raceCoinsBalance: " + response.raceCoinsBalance);
                Debug.Log("garageRevision: " + response.garageRevision);

                _state.SoftCurrency = response.raceCoinsBalance;
                _state.GarageRevision = response.garageRevision;

                if (view != null)
                    view.ShowStatus("Car purchased. Refreshing garage...");

                StartCoroutine(RefreshFlow());
                done = true;
            },
            error =>
            {
                Debug.LogError("BuyCarWithRaceCoins ERROR: " + error);

                if (view != null)
                    view.ShowStatus("Buy car error: " + error);

                done = true;
            }
        ));

        while (!done)
            yield return null;

        Debug.Log("=== BUY CAR FLOW END ===");
    }

    private IEnumerator PollGarageUntilBalanceChanges(int balanceSnapshot)
    {
        Debug.Log("=== BALANCE POLL START (snapshot=" + balanceSnapshot + ") ===");

        for (int i = 0; i < PaidInvoiceGarageRetryDelays.Length; i++)
        {
            yield return new WaitForSecondsRealtime(PaidInvoiceGarageRetryDelays[i]);
            yield return LoadGarageFlow();
            SaveProfileCache();

            if (_state.SoftCurrency != balanceSnapshot)
            {
                Debug.Log("Balance updated on poll attempt " + (i + 1) + ". New balance=" + _state.SoftCurrency);
                ShowPurchaseStatus("Balance updated: " + _state.SoftCurrency + " RC");
                _invoiceRefreshCoroutine = null;
                yield break;
            }

            Debug.LogWarning("Poll attempt " + (i + 1) + ": balance unchanged (" + _state.SoftCurrency + ")");
        }

        Debug.LogWarning("=== BALANCE POLL: all attempts exhausted, balance unchanged ===");
        _invoiceRefreshCoroutine = null;
    }

    private void ShowPurchaseStatus(string status)
    {
        if (view != null)
            view.ShowStatus(status);
    }

    public void OnInvoiceClosed(string status)
    {
        _state.LastInvoiceStatus = status;

        Debug.Log("=== INVOICE CLOSED ===");
        Debug.Log("Invoice status: " + status);

        if (string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase))
        {
            ShowPurchaseStatus("Payment confirmed!");
            return;
        }

        if (_invoiceRefreshCoroutine != null)
        {
            StopCoroutine(_invoiceRefreshCoroutine);
            _invoiceRefreshCoroutine = null;
        }

        ShowPurchaseStatus("Invoice closed: " + status);
        StartCoroutine(RefreshFlow());
    }

}
