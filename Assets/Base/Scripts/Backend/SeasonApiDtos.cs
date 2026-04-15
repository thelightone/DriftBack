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
    public string title;
    public string mapId;
    public int entryFee;
    public string startsAt;
    public string endsAt;
    public string status;
    public bool entered;
    public int bestScore;
    public int totalRaces;
}

[Serializable]
public class SeasonDetailDto
{
    public string seasonId;
    public string title;
    public string mapId;
    public int entryFee;
    public string startsAt;
    public string endsAt;
    public string status;
    public bool entered;
    public int bestScore;
    public int totalRaces;
}

[Serializable]
public class EnterSeasonResponse
{
    public bool success;
    public string seasonId;
    public string entryId;
    public int raceCoinsBalance;
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
