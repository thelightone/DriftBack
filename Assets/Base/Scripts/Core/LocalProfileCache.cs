using System;
using UnityEngine;

public static class LocalProfileCache
{
    private const string PlayerIdKey = "profile_playerId";
    private const string OwnedCarsKey = "profile_ownedCarsJson";
    private const string TrainingPointsKey = "profile_trainingPoints";
    private const string TournamentPointsKey = "profile_tournamentPoints";

    public static void Save(string playerId, string[] ownedCarIds, int trainingPoints, int tournamentPoints, int softCurrency)
    {
        PlayerPrefs.SetString(PlayerIdKey, playerId ?? "");
        PlayerPrefs.SetString(OwnedCarsKey, JsonHelper.ToJson(ownedCarIds ?? Array.Empty<string>()));
        PlayerPrefs.SetInt(TrainingPointsKey, trainingPoints);
        PlayerPrefs.SetInt(TournamentPointsKey, tournamentPoints);

        // softCurrency специально не сохраняем
        PlayerPrefs.Save();
    }

    public static CachedProfile Load()
    {
        return new CachedProfile
        {
            playerId = PlayerPrefs.GetString(PlayerIdKey, ""),
            ownedCarIds = JsonHelper.FromJson<string>(PlayerPrefs.GetString(OwnedCarsKey, "{\"items\":[]}")),
            trainingPoints = PlayerPrefs.GetInt(TrainingPointsKey, 0),
            tournamentPoints = PlayerPrefs.GetInt(TournamentPointsKey, 0),
            softCurrency = 0
        };
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(PlayerIdKey);
        PlayerPrefs.DeleteKey(OwnedCarsKey);
        PlayerPrefs.DeleteKey(TrainingPointsKey);
        PlayerPrefs.DeleteKey(TournamentPointsKey);
        PlayerPrefs.Save();
    }
}

[Serializable]
public class CachedProfile
{
    public string playerId;
    public string[] ownedCarIds;
    public int trainingPoints;
    public int tournamentPoints;
    public int softCurrency;
}

[Serializable]
public class JsonArrayWrapper<T>
{
    public T[] items;
}

public static class JsonHelper
{
    public static string ToJson<T>(T[] array)
    {
        return JsonUtility.ToJson(new JsonArrayWrapper<T> { items = array });
    }

    public static T[] FromJson<T>(string json)
    {
        if (string.IsNullOrEmpty(json))
            return Array.Empty<T>();

        var wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(json);
        return wrapper != null && wrapper.items != null ? wrapper.items : Array.Empty<T>();
    }
}