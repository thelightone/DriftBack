using System;
using UnityEngine;

public class PlayerCarSkinSelector : MonoBehaviour
{
    [Serializable]
    public class CarSkinEntry
    {
        public string carId;
        public GameObject skinObject;
    }

    [SerializeField] private string fallbackCarId = "car0";
    [SerializeField] private CarSkinEntry[] skins;

    public void ApplySelectedCar(string selectedCarId)
    {
        string targetCarId = string.IsNullOrWhiteSpace(selectedCarId) ? fallbackCarId : selectedCarId;
        bool found = false;

        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i] == null || skins[i].skinObject == null)
                continue;

            bool isTarget = skins[i].carId == targetCarId;
            skins[i].skinObject.SetActive(isTarget);

            if (isTarget)
                found = true;
        }

        if (!found)
        {
            Debug.LogWarning($"Selected car '{targetCarId}' not found. Falling back to '{fallbackCarId}'.");

            for (int i = 0; i < skins.Length; i++)
            {
                if (skins[i] == null || skins[i].skinObject == null)
                    continue;

                skins[i].skinObject.SetActive(skins[i].carId == fallbackCarId);
            }
        }
    }
}