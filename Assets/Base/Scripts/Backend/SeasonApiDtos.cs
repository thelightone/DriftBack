using System;

[Serializable]
public class SeasonsListResponse
{
    public SeasonSummaryDto[] seasons;
}

[Serializable]
public class SeasonSummaryDto
{
    public string seasonId;
    public string status;
}

[Serializable]
public class SeasonRaceStartResponse
{
    public string raceId;
    public string seed;
}

[Serializable]
public class SeasonRaceFinishRequest
{
    public string raceId;
    public string seed;
    public int score;
}

[Serializable]
public class SeasonRaceFinishResponse
{
    public string raceId;
    public int score;
    public bool isNewBest;
    public int bestScore;
}
