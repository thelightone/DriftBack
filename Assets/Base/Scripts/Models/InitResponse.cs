using System;

[Serializable]
public class InitResponse
{
    public bool isAuthorized;
    public string error;

    public string playerId;
    public string[] ownedCarIds;
    public bool isPremium;

    public int trainingPoints;
    public int tournamentPoints;

    public int softCurrency;
}