using System;

[Serializable]
public class InitRequest
{
    public string initData;
    public long telegramUserId;
    public string startParam;
    public string platform;
    public string appVersion;
}