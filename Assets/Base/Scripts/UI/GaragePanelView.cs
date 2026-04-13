using System.Collections.Generic;
using UnityEngine;

public class GaragePanelView : MonoBehaviour
{
    [Header("References")]
    public Transform contentRoot;
    public GarageCarItemView itemPrefab;

    private readonly List<GarageCarItemView> _spawnedItems = new();

    public void Rebuild(
        GarageCatalog catalog,
        string[] ownedCarIds,
        string selectedCarId,
        int playerCurrency,
        System.Action<CarDefinition> onCarAction)
    {
        Clear();

        if (catalog == null || catalog.cars == null || contentRoot == null || itemPrefab == null)
            return;

        for (int i = 0; i < catalog.cars.Length; i++)
        {
            var car = catalog.cars[i];
            if (car == null)
                continue;

            var item = Instantiate(itemPrefab, contentRoot);

            bool isOwned = Contains(ownedCarIds, car.carId);
            bool isSelected = isOwned && selectedCarId == car.carId;

            item.Setup(car, isOwned, isSelected, playerCurrency, onCarAction);
            _spawnedItems.Add(item);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _spawnedItems.Count; i++)
        {
            if (_spawnedItems[i] != null)
                Destroy(_spawnedItems[i].gameObject);
        }

        _spawnedItems.Clear();
    }

    private bool Contains(string[] array, string value)
    {
        if (array == null)
            return false;

        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == value)
                return true;
        }

        return false;
    }
}