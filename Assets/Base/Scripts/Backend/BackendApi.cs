using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class BackendApi
{
    private readonly string _baseUrl;

    public BackendApi(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        Debug.Log("BackendApi created. Base URL: " + _baseUrl);
    }

    public static string BuildTrainingRaceStartUrl(string baseUrl)
    {
        return baseUrl.TrimEnd('/') + "/v1/training-races/start";
    }

    public IEnumerator AuthTelegram(
        TelegramAuthRequest requestData,
        System.Action<TelegramAuthResponse> onSuccess,
        System.Action<string> onError)
    {
        string json = JsonUtility.ToJson(requestData);
        string url = _baseUrl + "/v1/auth/telegram";

        Debug.Log("=== AUTH TELEGRAM START ===");
        Debug.Log("POST " + url);
        Debug.Log("AuthTelegram request json: " + json);

        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        Debug.Log("AuthTelegram response code: " + request.responseCode);
        Debug.Log("AuthTelegram response text: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("AuthTelegram request error: " + request.error);
            onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
            yield break;
        }

        TelegramAuthResponse response = null;

        try
        {
            response = JsonUtility.FromJson<TelegramAuthResponse>(request.downloadHandler.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError("AuthTelegram parse error: " + e.Message);
            onError?.Invoke("AuthTelegram parse error: " + e.Message);
            yield break;
        }

        if (response == null)
        {
            onError?.Invoke("AuthTelegram response is null");
            yield break;
        }

        Debug.Log("=== AUTH TELEGRAM END ===");
        onSuccess?.Invoke(response);
    }

    public IEnumerator UpdateProfileNick(
        string accessToken,
        UpdateNickRequest requestData,
        Action<UpdateNickResponse> onSuccess,
        Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            onError?.Invoke("UNAUTHORIZED");
            yield break;
        }

        if (requestData == null || string.IsNullOrWhiteSpace(requestData.nick))
        {
            onError?.Invoke("INVALID_NICK");
            yield break;
        }

        string json = JsonUtility.ToJson(requestData);
        string url = _baseUrl + "/v1/profile/nick";

        Debug.Log("=== UPDATE PROFILE NICK START ===");
        Debug.Log("PUT " + url);
        Debug.Log("UpdateProfileNick request json: " + json);

        using var request = new UnityWebRequest(url, "PUT");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return request.SendWebRequest();

        string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
        Debug.Log("UpdateProfileNick response code: " + request.responseCode);
        Debug.Log("UpdateProfileNick response text: " + responseText);

        if (request.result != UnityWebRequest.Result.Success)
        {
            string errorCode = ResolveNickEndpointErrorCode(request.responseCode, responseText, request.error);
            Debug.LogError("UpdateProfileNick request error: " + errorCode);
            onError?.Invoke(errorCode);
            yield break;
        }

        UpdateNickResponse response = null;

        try
        {
            response = JsonUtility.FromJson<UpdateNickResponse>(responseText);
        }
        catch (Exception e)
        {
            Debug.LogError("UpdateProfileNick parse error: " + e.Message);
            onError?.Invoke("UpdateProfileNick parse error: " + e.Message);
            yield break;
        }

        if (response == null || string.IsNullOrWhiteSpace(response.nick))
        {
            onError?.Invoke("UpdateProfileNick response is invalid");
            yield break;
        }

        Debug.Log("=== UPDATE PROFILE NICK END ===");
        onSuccess?.Invoke(response);
    }

    public IEnumerator GetGarage(
        string accessToken,
        System.Action<GarageResponse> onSuccess,
        System.Action<string> onError)
    {
        string url = _baseUrl + "/v1/garage";

        Debug.Log("=== GET GARAGE START ===");
        Debug.Log("GET " + url);
        Debug.Log("AccessToken: " + accessToken);

        using var request = UnityWebRequest.Get(url);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return request.SendWebRequest();

        Debug.Log("GetGarage response code: " + request.responseCode);
        Debug.Log("GetGarage response text: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("GetGarage request error: " + request.error);
            onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
            yield break;
        }

        GarageResponse response = null;

        try
        {
            response = JsonUtility.FromJson<GarageResponse>(request.downloadHandler.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError("GetGarage parse error: " + e.Message);
            onError?.Invoke("GetGarage parse error: " + e.Message);
            yield break;
        }

        if (response == null)
        {
            onError?.Invoke("GetGarage response is null");
            yield break;
        }

        Debug.Log("=== GET GARAGE END ===");
        onSuccess?.Invoke(response);
    }

    public IEnumerator CreateCoinsPurchaseIntent(
        string accessToken,
        CreateCoinsPurchaseIntentRequest requestData,
        System.Action<CoinsPurchaseIntentResponse> onSuccess,
        System.Action<string> onError)
    {
        string json = JsonUtility.ToJson(requestData);
        string url = _baseUrl + "/v1/purchases/coins-intents";

        Debug.Log("=== CREATE COINS PURCHASE INTENT START ===");
        Debug.Log("POST " + url);
        Debug.Log("AccessToken: " + accessToken);
        Debug.Log("CreateCoinsPurchaseIntent request json: " + json);

        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return request.SendWebRequest();

        Debug.Log("CreateCoinsPurchaseIntent response code: " + request.responseCode);
        Debug.Log("CreateCoinsPurchaseIntent response text: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("CreateCoinsPurchaseIntent request error: " + request.error);
            onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
            yield break;
        }

        CoinsPurchaseIntentResponse response = null;

        try
        {
            response = JsonUtility.FromJson<CoinsPurchaseIntentResponse>(request.downloadHandler.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError("CreateCoinsPurchaseIntent parse error: " + e.Message);
            onError?.Invoke("CreateCoinsPurchaseIntent parse error: " + e.Message);
            yield break;
        }

        if (response == null)
        {
            onError?.Invoke("CreateCoinsPurchaseIntent response is null");
            yield break;
        }

        Debug.Log("=== CREATE COINS PURCHASE INTENT END ===");
        onSuccess?.Invoke(response);
    }

    public IEnumerator BuyCarWithRaceCoins(
        string accessToken,
        BuyCarRequest requestData,
        System.Action<BuyCarResponse> onSuccess,
        System.Action<string> onError)
    {
        string json = JsonUtility.ToJson(requestData);
        string url = _baseUrl + "/v1/purchases/buy-car";

        Debug.Log("=== BUY CAR WITH RACE COINS START ===");
        Debug.Log("POST " + url);
        Debug.Log("AccessToken: " + accessToken);
        Debug.Log("BuyCarWithRaceCoins request json: " + json);

        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return request.SendWebRequest();

        Debug.Log("BuyCarWithRaceCoins response code: " + request.responseCode);
        Debug.Log("BuyCarWithRaceCoins response text: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("BuyCarWithRaceCoins request error: " + request.error);
            onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
            yield break;
        }

        BuyCarResponse response = null;

        try
        {
            response = JsonUtility.FromJson<BuyCarResponse>(request.downloadHandler.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError("BuyCarWithRaceCoins parse error: " + e.Message);
            onError?.Invoke("BuyCarWithRaceCoins parse error: " + e.Message);
            yield break;
        }

        if (response == null)
        {
            onError?.Invoke("BuyCarWithRaceCoins response is null");
            yield break;
        }

        Debug.Log("=== BUY CAR WITH RACE COINS END ===");
        onSuccess?.Invoke(response);
    }

    public IEnumerator Health(
        System.Action<string> onSuccess,
        System.Action<string> onError)
    {
        string url = _baseUrl + "/health";

        Debug.Log("=== HEALTH CHECK START ===");
        Debug.Log("GET " + url);

        using var request = UnityWebRequest.Get(url);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        Debug.Log("Health response code: " + request.responseCode);
        Debug.Log("Health response text: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Health request error: " + request.error);
            onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
            yield break;
        }

        Debug.Log("=== HEALTH CHECK END ===");
        onSuccess?.Invoke(request.downloadHandler.text);
    }

    public IEnumerator GetSeasons(
        string accessToken,
        Action<SeasonsListResponse> onSuccess,
        Action<string> onError)
    {
        string url = _baseUrl + "/v1/seasons";

        Debug.Log("=== GET SEASONS START ===");
        Debug.Log("GET " + url);

        using var request = UnityWebRequest.Get(url);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return request.SendWebRequest();

        Debug.Log("GetSeasons response code: " + request.responseCode);
        Debug.Log("GetSeasons response text: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
            yield break;
        }

        SeasonsListResponse response = null;

        try
        {
            response = JsonUtility.FromJson<SeasonsListResponse>(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            onError?.Invoke("GetSeasons parse error: " + e.Message);
            yield break;
        }

        if (response == null)
        {
            onError?.Invoke("GetSeasons response is null");
            yield break;
        }

        Debug.Log("=== GET SEASONS END ===");
        onSuccess?.Invoke(response);
    }

    public IEnumerator GetSeasonDetail(
        string accessToken,
        string seasonId,
        Action<SeasonDetailDto> onSuccess,
        Action<string> onError)
    {
        string url = _baseUrl + "/v1/seasons/" + Uri.EscapeDataString(seasonId);

        Debug.Log("=== GET SEASON DETAIL START ===");
        Debug.Log("GET " + url);

        using var request = UnityWebRequest.Get(url);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return request.SendWebRequest();

        Debug.Log("GetSeasonDetail response code: " + request.responseCode);
        Debug.Log("GetSeasonDetail response text: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
            yield break;
        }

        SeasonDetailDto response = null;

        try
        {
            response = JsonUtility.FromJson<SeasonDetailDto>(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            onError?.Invoke("GetSeasonDetail parse error: " + e.Message);
            yield break;
        }

        if (response == null)
        {
            onError?.Invoke("GetSeasonDetail response is null");
            yield break;
        }

        Debug.Log("=== GET SEASON DETAIL END ===");
        onSuccess?.Invoke(response);
    }

    public IEnumerator EnterSeason(
        string accessToken,
        string seasonId,
        Action<EnterSeasonResponse> onSuccess,
        Action<string> onError)
    {
        string url = _baseUrl + "/v1/seasons/" + Uri.EscapeDataString(seasonId) + "/enter";
        byte[] body = Encoding.UTF8.GetBytes("{}");

        Debug.Log("=== ENTER SEASON START ===");
        Debug.Log("POST " + url);

        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return request.SendWebRequest();

        Debug.Log("EnterSeason response code: " + request.responseCode);
        Debug.Log("EnterSeason response text: " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            EnterSeasonResponse response = null;

            try
            {
                response = JsonUtility.FromJson<EnterSeasonResponse>(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                onError?.Invoke("EnterSeason parse error: " + e.Message);
                yield break;
            }

            Debug.Log("=== ENTER SEASON END ===");
            onSuccess?.Invoke(response);
            yield break;
        }

        if (request.responseCode == 409)
        {
            Debug.Log("EnterSeason: already entered (409), treating as success");
            Debug.Log("=== ENTER SEASON END (already entered) ===");
            onSuccess?.Invoke(null);
            yield break;
        }

        onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
    }

    public IEnumerator StartSeasonRace(
        string accessToken,
        string seasonId,
        Action<SeasonRaceStartResponse> onSuccess,
        Action<string> onError)
    {
        string url = _baseUrl + "/v1/seasons/" + Uri.EscapeDataString(seasonId) + "/races/start";
        byte[] body = Encoding.UTF8.GetBytes("{}");

        Debug.Log("=== SEASON RACE START ===");
        Debug.Log("POST " + url);

        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return request.SendWebRequest();

        Debug.Log("StartSeasonRace response code: " + request.responseCode);
        Debug.Log("StartSeasonRace response text: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
            yield break;
        }

        SeasonRaceStartResponse response = null;

        try
        {
            response = JsonUtility.FromJson<SeasonRaceStartResponse>(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            onError?.Invoke("StartSeasonRace parse error: " + e.Message);
            yield break;
        }

        if (response == null || string.IsNullOrWhiteSpace(response.raceId) ||
            string.IsNullOrWhiteSpace(response.seed))
        {
            onError?.Invoke("StartSeasonRace: invalid response");
            yield break;
        }

        Debug.Log("=== SEASON RACE START END ===");
        onSuccess?.Invoke(response);
    }

    public IEnumerator StartTrainingRace(
        string accessToken,
        Action<TrainingRaceStartResponse> onSuccess,
        Action<string> onError)
    {
        string url = BuildTrainingRaceStartUrl(_baseUrl);
        byte[] body = Encoding.UTF8.GetBytes("{}");

        Debug.Log("=== TRAINING RACE START ===");
        Debug.Log("POST " + url);

        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return request.SendWebRequest();

        Debug.Log("StartTrainingRace response code: " + request.responseCode);
        Debug.Log("StartTrainingRace response text: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
            yield break;
        }

        TrainingRaceStartResponse response = null;

        try
        {
            response = JsonUtility.FromJson<TrainingRaceStartResponse>(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            onError?.Invoke("StartTrainingRace parse error: " + e.Message);
            yield break;
        }

        if (response == null || string.IsNullOrWhiteSpace(response.raceId) ||
            string.IsNullOrWhiteSpace(response.seed) ||
            string.IsNullOrWhiteSpace(response.seasonId))
        {
            onError?.Invoke("StartTrainingRace: invalid response");
            yield break;
        }

        Debug.Log("=== TRAINING RACE START END ===");
        onSuccess?.Invoke(response);
    }

    public IEnumerator FinishSeasonRace(
        string accessToken,
        string seasonId,
        SeasonRaceFinishRequest requestData,
        Action<SeasonRaceFinishResponse> onSuccess,
        Action<string> onError)
    {
        string json = JsonUtility.ToJson(requestData);
        string url = _baseUrl + "/v1/seasons/" + Uri.EscapeDataString(seasonId) + "/races/finish";

        Debug.Log("=== SEASON RACE FINISH START ===");
        Debug.Log("POST " + url);
        Debug.Log("FinishSeasonRace request json: " + json);

        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return request.SendWebRequest();

        Debug.Log("FinishSeasonRace response code: " + request.responseCode);
        Debug.Log("FinishSeasonRace response text: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
            yield break;
        }

        SeasonRaceFinishResponse response = null;

        try
        {
            response = JsonUtility.FromJson<SeasonRaceFinishResponse>(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            onError?.Invoke("FinishSeasonRace parse error: " + e.Message);
            yield break;
        }

        if (response == null)
        {
            onError?.Invoke("FinishSeasonRace response is null");
            yield break;
        }

        Debug.Log("=== SEASON RACE FINISH END ===");
        onSuccess?.Invoke(response);
    }

    public IEnumerator FinishTrainingRace(
        string accessToken,
        string seasonId,
        TrainingRaceFinishRequest requestData,
        Action<TrainingRaceFinishResponse> onSuccess,
        Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            onError?.Invoke("UNAUTHORIZED");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(seasonId))
        {
            onError?.Invoke("SEASON_ID_REQUIRED");
            yield break;
        }

        string json = JsonUtility.ToJson(requestData);
        string url = _baseUrl + "/v1/seasons/" + Uri.EscapeDataString(seasonId) + "/training-races/finish";

        Debug.Log("=== TRAINING RACE FINISH START ===");
        Debug.Log("POST " + url);
        Debug.Log("FinishTrainingRace request json: " + json);

        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return request.SendWebRequest();

        Debug.Log("FinishTrainingRace response code: " + request.responseCode);
        Debug.Log("FinishTrainingRace response text: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
            yield break;
        }

        TrainingRaceFinishResponse response = null;

        try
        {
            response = JsonUtility.FromJson<TrainingRaceFinishResponse>(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            onError?.Invoke("FinishTrainingRace parse error: " + e.Message);
            yield break;
        }

        if (response == null)
        {
            onError?.Invoke("FinishTrainingRace response is null");
            yield break;
        }

        Debug.Log("=== TRAINING RACE FINISH END ===");
        onSuccess?.Invoke(response);
    }

    public IEnumerator GetSeasonLeaderboard(
        string accessToken,
        string seasonId,
        int limit,
        Action<LeaderboardResponse> onSuccess,
        Action<string> onError)
    {
        int safeLimit = Mathf.Clamp(limit, 1, 100);
        string url = _baseUrl + "/v1/seasons/" + Uri.EscapeDataString(seasonId) + "/leaderboard?limit=" + safeLimit;

        Debug.Log("=== GET SEASON LEADERBOARD START ===");
        Debug.Log("GET " + url);

        using var request = UnityWebRequest.Get(url);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return request.SendWebRequest();

        Debug.Log("GetSeasonLeaderboard response code: " + request.responseCode);
        Debug.Log("GetSeasonLeaderboard response text: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
            yield break;
        }

        LeaderboardResponse response = null;

        try
        {
            response = JsonUtility.FromJson<LeaderboardResponse>(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            onError?.Invoke("GetSeasonLeaderboard parse error: " + e.Message);
            yield break;
        }

        if (response == null)
        {
            onError?.Invoke("GetSeasonLeaderboard response is null");
            yield break;
        }

        Debug.Log("=== GET SEASON LEADERBOARD END ===");
        onSuccess?.Invoke(response);
    }

    private static string ResolveNickEndpointErrorCode(long statusCode, string responseText, string networkError)
    {
        string parsedCode = TryExtractApiErrorCode(responseText);
        if (!string.IsNullOrWhiteSpace(parsedCode))
            return parsedCode;

        if (statusCode == 400)
            return "INVALID_NICK";
        if (statusCode == 401)
            return "UNAUTHORIZED";
        if (statusCode == 404)
            return "NOT_FOUND";
        if (statusCode == 409)
            return "NICK_ALREADY_TAKEN";
        if (statusCode == 422)
            return "INSUFFICIENT_BALANCE";

        if (!string.IsNullOrWhiteSpace(networkError))
            return networkError;

        return "UNKNOWN_ERROR";
    }

    private static string TryExtractApiErrorCode(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return string.Empty;

        try
        {
            var direct = JsonUtility.FromJson<ApiErrorResponse>(responseText);
            if (direct != null && !string.IsNullOrWhiteSpace(direct.code))
                return direct.code.Trim();
        }
        catch
        {
        }

        try
        {
            var envelope = JsonUtility.FromJson<ApiErrorEnvelope>(responseText);
            if (envelope != null && envelope.error != null && !string.IsNullOrWhiteSpace(envelope.error.code))
                return envelope.error.code.Trim();
        }
        catch
        {
        }

        return string.Empty;
    }
}
