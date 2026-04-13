using UnityEngine;

public static class SelectedCarStorage
{
    private const string SelectedCarIdKey = "selected_car_id";

    public static void Save(string carId)
    {
        PlayerPrefs.SetString(SelectedCarIdKey, carId ?? "");
        PlayerPrefs.Save();
    }

    public static string Load()
    {
        return PlayerPrefs.GetString(SelectedCarIdKey, "");
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(SelectedCarIdKey);
        PlayerPrefs.Save();
    }
}