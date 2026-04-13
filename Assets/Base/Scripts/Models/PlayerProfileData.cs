using System;

[Serializable]
public class PlayerProfileData
{
    public string playerId;
    public string[] ownedCarIds;
    public bool isPremium;

    public int trainingPoints;
    public int tournamentPoints;

    public int softCurrency;
}