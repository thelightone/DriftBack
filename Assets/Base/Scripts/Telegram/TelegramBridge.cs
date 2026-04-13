using System.Runtime.InteropServices;
using UnityEngine;

public class TelegramBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern int TgIsAvailable();
    [DllImport("__Internal")] private static extern string TgGetInitData();
    [DllImport("__Internal")] private static extern string TgGetUserJson();
    [DllImport("__Internal")] private static extern string TgGetStartParam();
    [DllImport("__Internal")] private static extern string TgGetPlatform();
    [DllImport("__Internal")] private static extern string TgGetVersion();
    [DllImport("__Internal")] private static extern void TgReadyAndExpand();
    [DllImport("__Internal")] private static extern void TgOpenInvoice(string url, string gameObjectName, string callbackMethodName);
#endif

    public bool IsAvailable()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return TgIsAvailable() == 1;
#else
        return false;
#endif
    }

    public string GetInitData()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return TgGetInitData();
#else
        return "";
#endif
    }

    public TelegramUserData GetUser()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string json = TgGetUserJson();

        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonUtility.FromJson<TelegramUserData>(json);
#else
        return new TelegramUserData
        {
            id = 999999999,
            username = "editor_user",
            first_name = "Editor",
            last_name = "Mock",
            is_premium = false
        };
#endif
    }

    public string GetStartParam()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return TgGetStartParam();
#else
        return "";
#endif
    }

    public string GetPlatform()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return TgGetPlatform();
#else
        return "editor";
#endif
    }

    public string GetVersion()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return TgGetVersion();
#else
        return "editor";
#endif
    }

    public void ReadyAndExpand()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        TgReadyAndExpand();
#endif
    }

    public void OpenInvoice(string url, string gameObjectName, string callbackMethodName)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        TgOpenInvoice(url, gameObjectName, callbackMethodName);
#else
        Debug.Log("Invoice open requested in Editor: " + url);
#endif
    }
}