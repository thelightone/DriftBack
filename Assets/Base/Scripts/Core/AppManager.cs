using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private GarageCatalog garageCatalog;
    [SerializeField] private CurrencyPackCatalog currencyPackCatalog;
    [SerializeField] private SceneLoader sceneLoader;

    [Header("Backend")]
    [SerializeField] private string backendBaseUrl = "https://your-backend-url.com";

    private static readonly float[] PaidInvoiceGarageRetryDelays = { 1f, 2f, 2f, 2f };

    private AppState _state;
    private TelegramBridge _telegramBridge;
    private BackendApi _backendApi;
    private GarageResponse _lastGarageResponse;
    private Coroutine _invoiceRefreshCoroutine;
    private int _invoiceBalanceSnapshot;
    private string _tournamentHighScoreDisplay = "—";

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
                _state.PlayerId,
                _state.OwnedCarIds,
                _state.IsPremium,
                _state.TrainingPoints,
                _state.TournamentPoints,
                _state.SoftCurrency,
                _state.SelectedCarId,
                error,
                GetSelectedCarIcon()
            );
        }
    }

    private Sprite GetSelectedCarIcon()
    {
        if (garageCatalog == null || string.IsNullOrWhiteSpace(_state.SelectedCarId))
            return null;

        var def = garageCatalog.GetById(_state.SelectedCarId);
        return def != null ? def.icon : null;
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

    private void RebuildTournamentPanel()
    {
        if (tournamentPanelView == null)
            return;

        tournamentPanelView.ShowData(
            _state.SoftCurrency,
            tournamentEntryPrice,
            _state.SelectedCarId,
            _state.IsPremium,
            _tournamentHighScoreDisplay
        );
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
            _state.OwnedCarIds = response.profile.ownedCarIds ?? Array.Empty<string>();
            _state.GarageRevision = response.profile.garageRevision;
            _state.SoftCurrency = response.profile.raceCoinsBalance;
        }

        EnsureSelectedCarValid();

        Debug.Log("=== AUTH RESPONSE APPLIED ===");
        Debug.Log("IsAuthorized: " + _state.IsAuthorized);
        Debug.Log("PlayerId: " + _state.PlayerId);
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
        StartCoroutine(RefreshTournamentHighScoreCoroutine());

        if (view != null)
            view.ShowTournamentPanel();
    }

    public void OnCloseTournamentClicked()
    {
        if (view != null)
            view.ShowMainPanel();
    }

    public void OnBuyTournamentAccessClicked()
    {

        if (_state.SoftCurrency < tournamentEntryPrice)
        {
            if (view != null)
                view.ShowBuyCurrencyPanel();

            return;
        }

        _state.SoftCurrency -= tournamentEntryPrice;
        _state.IsPremium = true;

        SaveProfileCache();
        RebuildPanels();
        RefreshAllViews();

        if (view != null)
            view.ShowStatus("Tournament unlocked locally (temporary)");
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

        Debug.Log("Starting training race. PlayerId=" + _state.PlayerId);

        RaceSessionContext.StartTraining(
            _state.PlayerId,
            _state.InitData,
            _state.TelegramUser != null ? _state.TelegramUser.id : 0,
            backendBaseUrl
        );

        sceneLoader.StartTrainingGame();
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

    private IEnumerator RefreshTournamentHighScoreCoroutine()
    {
        if (tournamentPanelView == null)
            yield break;

        if (!_state.IsAuthorized || string.IsNullOrWhiteSpace(_state.AccessToken))
        {
            _tournamentHighScoreDisplay = "—";
            RebuildTournamentPanel();
            yield break;
        }

        _tournamentHighScoreDisplay = "…";
        RebuildTournamentPanel();

        string seasonId = null;
        string resolveErr = null;
        yield return ResolveActiveSeasonIdForTournament(
            id => seasonId = id,
            e => resolveErr = e);

        if (!string.IsNullOrEmpty(resolveErr) || string.IsNullOrEmpty(seasonId))
        {
            _tournamentHighScoreDisplay = "—";
            RebuildTournamentPanel();
            yield break;
        }

        string detailBody = null;
        string detailErr = null;
        yield return _backendApi.GetSeasonDetail(
            _state.AccessToken,
            seasonId,
            t => detailBody = t,
            e => detailErr = e);

        if (!string.IsNullOrEmpty(detailErr) || string.IsNullOrEmpty(detailBody))
        {
            _tournamentHighScoreDisplay = "—";
            RebuildTournamentPanel();
            yield break;
        }

        _tournamentHighScoreDisplay = SeasonDetailBestScoreParser.FormatHighScoreDisplay(detailBody);
        RebuildTournamentPanel();
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

        string enterErr = null;
        yield return _backendApi.EnterSeason(
            _state.AccessToken,
            seasonId,
            () => { },
            e => enterErr = e);

        if (!string.IsNullOrEmpty(enterErr))
        {
            NotifyTournamentFlow("Season enter: " + enterErr);
            yield break;
        }

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

    private IEnumerator RefreshGarageAfterPaidInvoiceFlow()
    {
        Debug.Log("=== PAID INVOICE REFRESH START ===");

        int balanceBeforeInvoice = _invoiceBalanceSnapshot;
        bool balanceUpdated = false;

        for (int i = 0; i < PaidInvoiceGarageRetryDelays.Length; i++)
        {
            float delay = PaidInvoiceGarageRetryDelays[i];
            int attemptNumber = i + 1;

            ShowPurchaseStatus($"Payment received. Syncing balance ({attemptNumber}/{PaidInvoiceGarageRetryDelays.Length})...");

            yield return new WaitForSecondsRealtime(delay);
            yield return LoadGarageFlow();

            SaveProfileCache();

            if (_state.SoftCurrency != balanceBeforeInvoice)
            {
                balanceUpdated = true;
                Debug.Log("Paid invoice refresh detected updated balance on attempt " + attemptNumber);
                break;
            }

            Debug.LogWarning(
                "Paid invoice refresh attempt " + attemptNumber +
                " finished with unchanged balance. Snapshot=" + balanceBeforeInvoice +
                ", Current=" + _state.SoftCurrency);
        }

        if (balanceUpdated)
            ShowPurchaseStatus("Payment confirmed. Balance updated.");
        else
            ShowPurchaseStatus("Payment confirmed, but balance sync is still pending. Try refresh in a moment.");

        _invoiceRefreshCoroutine = null;
        Debug.Log("=== PAID INVOICE REFRESH END ===");
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

        if (_invoiceRefreshCoroutine != null)
        {
            StopCoroutine(_invoiceRefreshCoroutine);
            _invoiceRefreshCoroutine = null;
        }

        if (string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase))
        {
            ShowPurchaseStatus("Invoice paid. Waiting for backend balance sync...");
            _invoiceRefreshCoroutine = StartCoroutine(RefreshGarageAfterPaidInvoiceFlow());
            return;
        }

        ShowPurchaseStatus("Invoice closed: " + status + ". Refreshing profile...");
        StartCoroutine(RefreshFlow());
    }

}