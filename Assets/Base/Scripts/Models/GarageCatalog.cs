using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GarageCatalog", menuName = "Game/Garage Catalog")]
public class GarageCatalog : ScriptableObject
{
    public CarDefinition[] cars;

    public CarDefinition GetById(string carId)
    {
        if (cars == null)
            return null;

        return cars.FirstOrDefault(x => x != null && x.carId == carId);
    }
}