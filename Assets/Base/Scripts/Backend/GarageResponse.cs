using System;

[Serializable]
public class GarageResponse
{
    public int garageRevision;
    public GarageCarDto[] cars;
    public int raceCoinsBalance;
}