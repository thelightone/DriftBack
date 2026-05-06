using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TournamentPanelView : MonoBehaviour
{
    [Header("Balance")]
    public TMP_Text tournamentBalanceText;

    [Header("Texts")]
    public TMP_Text descriptionText;
    public TMP_Text entryPriceText;
    [Tooltip("Лучший счёт игрока в текущем турнире (сезоне), с бэкенда.")]
    public TMP_Text tournamentHighScoreText;
    [SerializeField] private string tournamentHighScoreLabelPrefix = "";

    [Tooltip("Текущее место игрока в рейтинге сезона (передаётся уже с префиксом #).")]
    public TMP_Text tournamentRatingPlaceText;

    [Tooltip("Очки лидера рейтинга (1-е место).")]
    public TMP_Text firstPlaceScoreText;

    [Tooltip("Период турнира: «дд.мм-дд.мм» (строка уже собрана).")]
    public TMP_Text tournamentDatesText;

    private string _primaryBalanceLine;

    [Header("Buttons")]
    public Button openBuyCurrencyButton;
    public Button closeButton;
    public Button buyAccessButton;
    public Button startTournamentButton;

    public void ShowData(
        int softCurrency,
        int entryPrice,
        string selectedCarId,
        bool isPremium,
        string tournamentHighScoreDisplay,
        string ratingPlaceDisplay,
        string firstPlaceScoreDisplay,
        string tournamentDatesRangeDisplay
    )
    {
        _primaryBalanceLine = softCurrency + " RC";
        if (tournamentBalanceText != null)
            tournamentBalanceText.text = _primaryBalanceLine;

        if (descriptionText != null)
            descriptionText.text = "Pay coins to unlock tournament access and compete for the best result";

        if (entryPriceText != null)
            entryPriceText.text = "Entry Price: " + entryPrice + " Coins";

        if (tournamentHighScoreText != null)
        {
            string value = string.IsNullOrEmpty(tournamentHighScoreDisplay) ? "—" : tournamentHighScoreDisplay;
            tournamentHighScoreText.text = string.IsNullOrEmpty(tournamentHighScoreLabelPrefix)
                ? value
                : tournamentHighScoreLabelPrefix + " " + value;
        }

        if (tournamentRatingPlaceText != null)
            tournamentRatingPlaceText.text = string.IsNullOrWhiteSpace(ratingPlaceDisplay) ? "—" : ratingPlaceDisplay;

        if (firstPlaceScoreText != null)
            firstPlaceScoreText.text = string.IsNullOrWhiteSpace(firstPlaceScoreDisplay) ? "—" : firstPlaceScoreDisplay;

        if (tournamentDatesText != null)
            tournamentDatesText.text = string.IsNullOrWhiteSpace(tournamentDatesRangeDisplay) ? "—" : tournamentDatesRangeDisplay;

        if (buyAccessButton != null)
            buyAccessButton.gameObject.SetActive(!isPremium);

        if (startTournamentButton != null)
            startTournamentButton.gameObject.SetActive(isPremium);
    }

    public void SetTournamentFlowMessage(string message)
    {
        if (tournamentBalanceText == null)
            return;

        string baseLine = string.IsNullOrEmpty(_primaryBalanceLine) ? "—" : _primaryBalanceLine;

        if (string.IsNullOrEmpty(message))
        {
            tournamentBalanceText.text = baseLine;
            return;
        }

        tournamentBalanceText.text = baseLine + "\n" + message;
    }

    private string Safe(string value)
    {
        return string.IsNullOrEmpty(value) ? "-" : value;
    }
}