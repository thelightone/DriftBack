public class AppState
{
    public bool TelegramAvailable;
    public string InitData;
    public string StartParam;
    public string Platform;
    public string AppVersion;

    public TelegramUserData TelegramUser;

    public bool IsAuthorized;

    public string PlayerId;
    public string[] OwnedCarIds;
    public bool IsPremium;

    public int TrainingPoints;
    public int TournamentPoints;

    public int SoftCurrency;

    public string SelectedCarId;
    public string LastInvoiceStatus;

    public string AccessToken;
    public int GarageRevision;
}