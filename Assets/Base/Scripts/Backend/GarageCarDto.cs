using System;

[Serializable]
public class GarageCarDto
{
    public string carId;
    public string title;
    public bool owned;
    public PriceDto price;
    public bool canBuy;
}