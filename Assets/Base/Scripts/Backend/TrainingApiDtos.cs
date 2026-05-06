using System;

[Serializable]
public class TrainingRaceFinishRequest
{
    public float lapTimeSeconds;
    public int coinsEarned;
}

[Serializable]
public class TrainingRaceFinishResponse
{
    public int coinsGranted;
    public int raceCoinsBalance;
}
