using UnityEngine;

[CreateAssetMenu(fileName = "CarDefinition", menuName = "Game/Car Definition")]
public class CarDefinition : ScriptableObject
{
    public string carId;
    public string displayName;
    public int softCurrencyPrice;
    public Sprite icon;
}