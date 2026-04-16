using System;

[Serializable]
public class LeaderboardResponse
{
    public string seasonId;
    public LeaderboardEntryDto[] entries;
    public LeaderboardEntryDto currentPlayer;
    public int totalParticipants;
}

[Serializable]
public class LeaderboardEntryDto
{
    public int rank;
    public string userId;
    public string username;
    public string firstName;
    public int bestScore;
    public int totalRaces;
}

