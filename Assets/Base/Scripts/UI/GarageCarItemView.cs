using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GarageCarItemView : MonoBehaviour
{
    [Header("Catalog")]
    [Tooltip("Same CarDefinition asset as in Garage Catalog — which car this slot shows.")]
    public CarDefinition carBinding;

    [Header("UI")]
    public Image carIcon;
    public TMP_Text carNameText;
    public TMP_Text carPriceText;
    [Tooltip("If set, this object is disabled when the car is owned; price text is shown only while it is active. If empty, Car Price Text is cleared instead.")]
    public GameObject priceRowRoot;
    public Button actionButton;
    public TMP_Text actionButtonText;

    private CarDefinition _car;
    private System.Action<CarDefinition> _onAction;
    private bool _suppressClick;

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

        if (priceRowRoot != null)
        {
            priceRowRoot.SetActive(!isOwned);
            if (!isOwned && carPriceText != null)
                carPriceText.text = $"{car.softCurrencyPrice} RC";
        }
        else if (carPriceText != null)
        {
            if (isOwned)
                carPriceText.text = "";
            else
                carPriceText.text = $"{car.softCurrencyPrice} RC";
        }

        _suppressClick = isOwned && isSelected;

        if (isOwned)
            actionButtonText.text = isSelected ? "ВЫБРАНО" : "ВЫБРАТЬ";
        else
            actionButtonText.text = "КУПИТЬ";

        if (actionButton != null)
            actionButton.interactable = true;

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnActionClicked);
    }

    private void OnActionClicked()
    {
        Debug.Log("Garage item clicked: " + (_car != null ? _car.carId : "NULL"));

        if (_suppressClick)
            return;

        _onAction?.Invoke(_car);
    }
}