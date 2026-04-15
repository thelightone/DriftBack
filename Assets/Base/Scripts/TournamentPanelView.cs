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
    [SerializeField] private string tournamentHighScoreLabelPrefix = "Best score:";

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
        string tournamentHighScoreDisplay
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
            string prefix = string.IsNullOrEmpty(tournamentHighScoreLabelPrefix) ? "Best score:" : tournamentHighScoreLabelPrefix;
            tournamentHighScoreText.text = prefix + " " + value;
        }

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