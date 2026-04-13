using System;

[Serializable]
public class SubmitRaceResultRequest
{
    public string initData;
    public long telegramUserId;
    public string playerId;
    public int score;
    public float timeSeconds;
}