using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyPackItemView : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public TMP_Text titleText;
    public TMP_Text starsPriceText;
    public Button buyButton;

    private CurrencyPackDefinition _pack;
    private System.Action<CurrencyPackDefinition> _onBuy;

    public void Setup(CurrencyPackDefinition pack, System.Action<CurrencyPackDefinition> onBuy)
    {
        Debug.Log("CurrencyPackItemView.Setup called: " + (pack != null ? pack.productId : "NULL"));

        _pack = pack;
        _onBuy = onBuy;

        if (_pack == null)
        {
            Debug.LogError("CurrencyPackItemView.Setup: pack is NULL");
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = pack.icon;
            iconImage.enabled = pack.icon != null;
        }

        if (titleText != null)
            titleText.text = pack.displayName;

        if (starsPriceText != null)
            starsPriceText.text = pack.starsPrice.ToString();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
            Debug.Log("CurrencyPackItemView.Setup: buyButton listener attached for " + pack.productId);
        }
        else
        {
            Debug.LogError("CurrencyPackItemView.Setup: buyButton is NULL for " + pack.productId);
        }
    }

    private void OnBuyClicked()
    {
        Debug.Log("Currency pack button clicked: " + (_pack != null ? _pack.productId : "NULL"));
        _onBuy?.Invoke(_pack);
    }
}