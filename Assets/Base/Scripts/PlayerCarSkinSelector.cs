using System;
using UnityEngine;

[DefaultExecutionOrder(-40)]
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

    private void Start()
    {
        // Race scenes (e.g. Level1) load without RaceBootstrap; apply saved selection here.
        ApplySelectedCar(SelectedCarStorage.Load());
    }

    public void ApplySelectedCar(string selectedCarId)
    {
        if (skins == null || skins.Length == 0)
        {
            Debug.LogWarning("PlayerCarSkinSelector: skins list is empty.");
            return;
        }

        string targetCarId = NormalizeId(string.IsNullOrWhiteSpace(selectedCarId) ? fallbackCarId : selectedCarId);
        string fallback = NormalizeId(fallbackCarId);
        bool found = false;
        GameObject activeSkinRoot = null;

        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i] == null || skins[i].skinObject == null)
                continue;

            bool isTarget = IdEquals(skins[i].carId, targetCarId);
            skins[i].skinObject.SetActive(isTarget);

            if (isTarget)
            {
                found = true;
                activeSkinRoot = skins[i].skinObject;
            }
        }

        if (!found)
        {
            Debug.LogWarning($"Selected car '{targetCarId}' not found. Falling back to '{fallback}'.");

            for (int i = 0; i < skins.Length; i++)
            {
                if (skins[i] == null || skins[i].skinObject == null)
                    continue;

                bool useFallback = IdEquals(skins[i].carId, fallback);
                skins[i].skinObject.SetActive(useFallback);
                if (useFallback)
                    activeSkinRoot = skins[i].skinObject;
            }
        }

        NotifyGameControllerOfActiveSkin(activeSkinRoot);
    }

    static void NotifyGameControllerOfActiveSkin(GameObject skinRoot)
    {
        if (skinRoot == null || GameController.Instance == null)
            return;

        var car = skinRoot.GetComponent<CarController>()
            ?? skinRoot.GetComponentInChildren<CarController>(true);
        if (car != null)
            GameController.Instance.SwitchToPlayerCar(car);
    }

    private static string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }

    private static bool IdEquals(string a, string b)
    {
        return string.Equals(NormalizeId(a), NormalizeId(b), StringComparison.OrdinalIgnoreCase);
    }
}