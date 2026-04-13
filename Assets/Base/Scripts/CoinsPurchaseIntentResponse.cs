using System;

[Serializable]
public class CoinsPurchaseIntentResponse
{
    public string purchaseId;
    public string status;
    public string invoiceUrl;
    public string expiresAt;
    public PriceDto price;
    public int coinsAmount;
}