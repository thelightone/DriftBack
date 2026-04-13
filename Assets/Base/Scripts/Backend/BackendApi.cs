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
}