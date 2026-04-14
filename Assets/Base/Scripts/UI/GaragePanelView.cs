using UnityEngine;

public class GaragePanelView : MonoBehaviour
{
    [Header("Pre-placed cards")]
    [Tooltip("Leave empty to auto-collect all GarageCarItemView under Content Root at startup (hierarchy order).")]
    public GarageCarItemView[] carItems;

    [Header("Optional")]
    [Tooltip("Parent for auto-collect when Car Items is empty.")]
    public Transform contentRoot;

    private void Awake()
    {
        EnsureCarItemsCollected();
    }

    private void EnsureCarItemsCollected()
    {
        if ((carItems == null || carItems.Length == 0) && contentRoot != null)
            carItems = contentRoot.GetComponentsInChildren<GarageCarItemView>(true);
    }

    public void Rebuild(
        GarageCatalog catalog,
        string[] ownedCarIds,
        string selectedCarId,
        int playerCurrency,
        System.Action<CarDefinition> onCarAction)
    {
        EnsureCarItemsCollected();

        if (carItems == null || carItems.Length == 0)
        {
            Debug.LogWarning("GaragePanelView: no car cards — assign Car Items or Content Root with GarageCarItemView children.");
            return;
        }

        for (int i = 0; i < carItems.Length; i++)
        {
            var item = carItems[i];
            if (item == null)
                continue;

            if (item.carBinding == null)
            {
                item.gameObject.SetActive(false);
                continue;
            }

            var car = catalog != null ? catalog.GetById(item.carBinding.carId) : null;
            if (car == null)
                car = item.carBinding;

            bool isOwned = Contains(ownedCarIds, car.carId);
            bool isSelected = isOwned && selectedCarId == car.carId;

            item.gameObject.SetActive(true);
            item.Setup(car, isOwned, isSelected, playerCurrency, onCarAction);
        }
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