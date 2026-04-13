using UnityEngine;

public class RaceBootstrap : MonoBehaviour
{
    [SerializeField] private PlayerCarSkinSelector carSelector;

    private void Start()
    {
        string selectedCarId = SelectedCarStorage.Load();

        carSelector.ApplySelectedCar(selectedCarId);

        Debug.Log("Selected car applied: " + selectedCarId);
    }
}