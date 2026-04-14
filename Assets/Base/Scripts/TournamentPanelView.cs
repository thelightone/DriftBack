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
    public TMP_Text selectedCarInfoText;
    public TMP_Text tournamentAccessText;
    public TMP_Text tournamentStatusText;

    [Header("Buttons")]
    public Button openBuyCurrencyButton;
    public Button closeButton;
    public Button buyAccessButton;
    public Button startTournamentButton;

    public void ShowData(
        int softCurrency,
        int entryPrice,
        string selectedCarId,
        bool isPremium
    )
    {
        if (tournamentBalanceText != null)
            tournamentBalanceText.text = softCurrency + " RC";

        if (descriptionText != null)
            descriptionText.text = "Pay coins to unlock tournament access and compete for the best result";

        if (entryPriceText != null)
            entryPriceText.text = "Entry Price: " + entryPrice + " Coins";

        if (selectedCarInfoText != null)
            selectedCarInfoText.text = "Selected Car: " + Safe(selectedCarId);

        if (tournamentAccessText != null)
            tournamentAccessText.text = "Access: " + (isPremium ? "Unlocked" : "Not unlocked");

        if (buyAccessButton != null)
            buyAccessButton.gameObject.SetActive(!isPremium);

        if (startTournamentButton != null)
            startTournamentButton.gameObject.SetActive(isPremium);
    }

    public void ShowStatus(string text)
    {
        if (tournamentStatusText != null)
            tournamentStatusText.text = text;
    }

    private string Safe(string value)
    {
        return string.IsNullOrEmpty(value) ? "-" : value;
    }
}