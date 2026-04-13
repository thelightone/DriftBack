using System;

[Serializable]
public class CarPurchaseIntentResponse
{
    public string purchaseId;
    public string status;
    public string invoiceUrl;
    public string expiresAt;
    public PriceDto price;
}