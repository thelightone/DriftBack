using System;

[Serializable]
public class TelegramAuthResponse
{
    public string accessToken;
    public int expiresInSec;
    public TelegramProfile profile;
}