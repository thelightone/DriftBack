public static class RaceSessionContext
{
    public static RaceMode CurrentMode = RaceMode.Training;

    public static string PlayerId;
    public static string InitData;
    public static long TelegramUserId;
    public static string BackendBaseUrl;

    public static void StartTraining(string playerId, string initData, long telegramUserId, string backendBaseUrl)
    {
        CurrentMode = RaceMode.Training;
        PlayerId = playerId ?? "";
        InitData = initData ?? "";
        TelegramUserId = telegramUserId;
        BackendBaseUrl = backendBaseUrl ?? "";
    }

    public static void StartTournament(string playerId, string initData, long telegramUserId, string backendBaseUrl)
    {
        CurrentMode = RaceMode.Tournament;
        PlayerId = playerId ?? "";
        InitData = initData ?? "";
        TelegramUserId = telegramUserId;
        BackendBaseUrl = backendBaseUrl ?? "";
    }

    public static bool IsTraining => CurrentMode == RaceMode.Training;
    public static bool IsTournament => CurrentMode == RaceMode.Tournament;
}