using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GarageCarItemView : MonoBehaviour
{
    [Header("UI")]
    public Image carIcon;
    public TMP_Text carNameText;
    public TMP_Text carPriceText;
    public TMP_Text stateText;
    public Button actionButton;
    public TMP_Text actionButtonText;

    private CarDefinition _car;
    private System.Action<CarDefinition> _onAction;

    public void Setup(
        CarDefinition car,
        bool isOwned,
        bool isSelected,
        int playerCurrency,
        System.Action<CarDefinition> onAction)
    {
        _car = car;
        _onAction = onAction;

        if (carIcon != null)
            carIcon.sprite = car.icon;

        if (carNameText != null)
            carNameText.text = car.displayName;

        if (isOwned)
        {
            carPriceText.text = "";
            stateText.text = isSelected ? "Selected" : "Owned";
            actionButtonText.text = isSelected ? "Selected" : "Select";
            actionButton.interactable = !isSelected;
        }
        else
        {
            carPriceText.text = $"{car.softCurrencyPrice} Coins";
            stateText.text = playerCurrency >= car.softCurrencyPrice ? "Can Buy" : "Not enough currency";
            actionButtonText.text = "Buy";
            actionButton.interactable = true;
        }

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnActionClicked);
    }

    private void OnActionClicked()
    {
        Debug.Log("Garage item clicked: " + (_car != null ? _car.carId : "NULL"));

        _onAction?.Invoke(_car);
    }
}