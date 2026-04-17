using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class MainScreenView : MonoBehaviour
{
    [Header("Telegram")]
    public TMP_Text telegramAvailableText;
    public TMP_Text telegramUserIdText;
    public TMP_Text telegramUsernameText;
    public TMP_Text telegramFirstNameText;
    public TMP_Text telegramInitDataText;

    [Header("Profile - Main Panel")]
    public TMP_Text playerIdText;
    public TMP_Text ownedCarsText;
    public TMP_Text premiumText;
    public TMP_Text trainingPointsText;
    public TMP_Text tournamentPointsText;
    public TMP_Text currentTournamentRecordText;
    public TMP_Text tournamentPlaceText;
    public TMP_Text softCurrencyText;
    public TMP_Text selectedCarText;
    public Image selectedCarPreviewImage;

    [Header("Profile - Garage Panel")]
    public TMP_Text garageBalanceText;

    [Header("Profile - Buy Currency Panel")]
    public TMP_Text buyCurrencyBalanceText;

    [Header("Status")]
    public TMP_Text paymentStatusText;

    [Header("Panels")]
    public GameObject mainPanelRoot;
    public GameObject garagePanelRoot;
    public GameObject buyCurrencyPanelRoot;
    public GameObject tournamentPanelRoot;
    public GameObject leaderboardPanelRoot;

    public void ShowTelegramData(bool available, TelegramUserData user, string initData)
    {
        if (telegramAvailableText != null)
            telegramAvailableText.text = "Telegram: " + available;

        if (user != null)
        {
            if (telegramUserIdText != null)
                telegramUserIdText.text = "ID: " + user.id;

            if (telegramUsernameText != null)
                telegramUsernameText.text = "@" + Safe(user.username);

            if (telegramFirstNameText != null)
                telegramFirstNameText.text = Safe(user.first_name);
        }
        else
        {
            if (telegramUserIdText != null) telegramUserIdText.text = "ID: -";
            if (telegramUsernameText != null) telegramUsernameText.text = "-";
            if (telegramFirstNameText != null) telegramFirstNameText.text = "-";
        }

        if (telegramInitDataText != null)
            telegramInitDataText.text = string.IsNullOrEmpty(initData) ? "InitData: empty" : "InitData: ok";
    }

    public void ShowProfile(
        bool isAuthorized,
        string playerId,
        string[] ownedCarIds,
        bool isPremium,
        int trainingPoints,
        int tournamentPoints,
        string currentTournamentRecord,
        string tournamentPlace,
        int softCurrency,
        string selectedCarId,
        string error,
        Sprite selectedCarIcon
    )
    {
        if (playerIdText != null)
            playerIdText.text = "Player: " + Safe(playerId);

        if (ownedCarsText != null)
            ownedCarsText.text = "Cars: " + FormatCars(ownedCarIds);

        if (premiumText != null)
            premiumText.text = "Premium: " + (isPremium ? "Yes" : "No");

        if (trainingPointsText != null)
            trainingPointsText.text = trainingPoints.ToString();

        if (tournamentPointsText != null)
            tournamentPointsText.text = tournamentPoints.ToString();

        if (currentTournamentRecordText != null)
            currentTournamentRecordText.text = string.IsNullOrWhiteSpace(currentTournamentRecord) ? "—" : currentTournamentRecord;

        if (tournamentPlaceText != null)
            tournamentPlaceText.text = string.IsNullOrWhiteSpace(tournamentPlace) ? "—" : tournamentPlace;

        if (softCurrencyText != null)
            softCurrencyText.text = softCurrency + " RC";

        if (selectedCarText != null)
            selectedCarText.text = "Selected: " + Safe(selectedCarId);

        if (selectedCarPreviewImage != null)
        {
            selectedCarPreviewImage.sprite = selectedCarIcon;
            selectedCarPreviewImage.enabled = selectedCarIcon != null;
        }

        if (garageBalanceText != null)
            garageBalanceText.text = softCurrency + " RC";

        if (buyCurrencyBalanceText != null)
            buyCurrencyBalanceText.text = softCurrency + " RC";

        if (!string.IsNullOrEmpty(error))
            ShowStatus(error);
        else
            ShowStatus(isAuthorized ? "Profile loaded" : "Not authorized");
    }

    public void ShowStatus(string text)
    {
        Debug.Log("MainScreenView.ShowStatus: " + text);

        if (paymentStatusText != null)
            paymentStatusText.text = text;
    }

    public void ShowMainPanel()
    {
        Debug.Log("ShowMainPanel called");

        SetPanel(mainPanelRoot, true);
        SetPanel(garagePanelRoot, false);
        SetPanel(buyCurrencyPanelRoot, false);
        SetPanel(tournamentPanelRoot, false);
        SetPanel(leaderboardPanelRoot, false);
    }

    public void ShowGaragePanel()
    {
        Debug.Log("ShowGaragePanel called");

        SetPanel(mainPanelRoot, false);
        SetPanel(garagePanelRoot, true);
        SetPanel(buyCurrencyPanelRoot, false);
        SetPanel(tournamentPanelRoot, false);
        SetPanel(leaderboardPanelRoot, false);
    }

    public void ShowBuyCurrencyPanel()
    {
        Debug.Log("ShowBuyCurrencyPanel called");
        Debug.Log("buyCurrencyPanelRoot: " + (buyCurrencyPanelRoot != null ? buyCurrencyPanelRoot.name : "NULL"));

        SetPanel(mainPanelRoot, false);
        SetPanel(garagePanelRoot, false);
        SetPanel(buyCurrencyPanelRoot, true);
        SetPanel(tournamentPanelRoot, false);
        SetPanel(leaderboardPanelRoot, false);
    }

    public void ShowTournamentPanel()
    {
        Debug.Log("ShowTournamentPanel called");

        SetPanel(mainPanelRoot, false);
        SetPanel(garagePanelRoot, false);
        SetPanel(buyCurrencyPanelRoot, false);
        SetPanel(tournamentPanelRoot, true);
        SetPanel(leaderboardPanelRoot, false);
    }

    public void ShowLeaderboardPanel()
    {
        Debug.Log("ShowLeaderboardPanel called");

        SetPanel(mainPanelRoot, false);
        SetPanel(garagePanelRoot, false);
        SetPanel(buyCurrencyPanelRoot, false);
        SetPanel(tournamentPanelRoot, false);
        SetPanel(leaderboardPanelRoot, true);
    }

    private void SetPanel(GameObject panel, bool state)
    {
        Debug.Log("SetPanel: " + (panel != null ? panel.name : "NULL") + " -> " + state);

        if (panel != null)
            panel.SetActive(state);
    }

    private string Safe(string value)
    {
        return string.IsNullOrEmpty(value) ? "-" : value;
    }

    private string FormatCars(string[] cars)
    {
        if (cars == null || cars.Length == 0)
            return "None";

        return string.Join(", ", cars.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}