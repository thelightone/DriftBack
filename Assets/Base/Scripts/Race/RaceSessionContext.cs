public static class RaceSessionContext
{
    public static RaceMode CurrentMode = RaceMode.Training;

    public static string PlayerId;
    public static string InitData;
    public static long TelegramUserId;
    public static string BackendBaseUrl;

    public static string AccessToken;
    public static string SeasonId;
    public static string RaceId;
    public static string Seed;
    public static bool BackendRacePrepared;

    public static void StartTraining(string playerId, string initData, long telegramUserId, string backendBaseUrl)
    {
        CurrentMode = RaceMode.Training;
        PlayerId = playerId ?? "";
        InitData = initData ?? "";
        TelegramUserId = telegramUserId;
        BackendBaseUrl = backendBaseUrl ?? "";
        ClearBackendTournamentState();
    }

    public static void StartTournament(string playerId, string initData, long telegramUserId, string backendBaseUrl)
    {
        CurrentMode = RaceMode.Tournament;
        PlayerId = playerId ?? "";
        InitData = initData ?? "";
        TelegramUserId = telegramUserId;
        BackendBaseUrl = backendBaseUrl ?? "";
        ClearBackendTournamentState();
    }

    public static void BeginTournamentRace(
        string accessToken,
        string seasonId,
        string raceId,
        string seed,
        string playerId,
        string initData,
        long telegramUserId,
        string backendBaseUrl)
    {
        CurrentMode = RaceMode.Tournament;
        AccessToken = accessToken ?? "";
        SeasonId = seasonId ?? "";
        RaceId = raceId ?? "";
        Seed = seed ?? "";
        PlayerId = playerId ?? "";
        InitData = initData ?? "";
        TelegramUserId = telegramUserId;
        BackendBaseUrl = backendBaseUrl ?? "";
        BackendRacePrepared = true;
    }

    public static void MergeBridgeSnapshot(string playerId, string initData, long telegramUserId, string backendBaseUrl)
    {
        PlayerId = playerId ?? "";
        InitData = initData ?? "";
        TelegramUserId = telegramUserId;
        BackendBaseUrl = backendBaseUrl ?? "";
    }

    static void ClearBackendTournamentState()
    {
        AccessToken = "";
        SeasonId = "";
        RaceId = "";
        Seed = "";
        BackendRacePrepared = false;
    }

    public static bool IsTraining => CurrentMode == RaceMode.Training;
    public static bool IsTournament => CurrentMode == RaceMode.Tournament;
}
