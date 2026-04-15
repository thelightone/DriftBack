using UnityEngine;

public class BuyCurrencyPanelView : MonoBehaviour
{
    [Tooltip("Explicit cards (order must match CurrencyPackCatalog.packs). If empty, children under contentRoot are used.")]
    [SerializeField] private CurrencyPackItemView[] packItemViews;

    [Tooltip("Parent for scene-placed pack cards. Used only when packItemViews is empty.")]
    [SerializeField] private Transform contentRoot;

    public void Rebuild(CurrencyPackCatalog catalog, System.Action<CurrencyPackDefinition> onBuy)
    {
        Debug.Log("BuyCurrencyPanelView.Rebuild called");

        if (catalog == null)
        {
            Debug.LogError("BuyCurrencyPanelView: catalog is NULL");
            return;
        }

        if (catalog.packs == null)
        {
            Debug.LogError("BuyCurrencyPanelView: catalog.packs is NULL");
            return;
        }

        var items = ResolvePackItems();
        if (items == null || items.Length == 0)
        {
            Debug.LogError("BuyCurrencyPanelView: no cards — assign packItemViews or place CurrencyPackItemView under contentRoot.");
            return;
        }

        Debug.Log("BuyCurrencyPanelView: packs count = " + catalog.packs.Length + ", item views = " + items.Length);

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item == null)
                continue;

            if (i < catalog.packs.Length && catalog.packs[i] != null)
            {
                item.gameObject.SetActive(true);
                item.Setup(catalog.packs[i], onBuy);
            }
            else
                item.gameObject.SetActive(false);
        }

        if (catalog.packs.Length > items.Length)
        {
            Debug.LogWarning(
                "BuyCurrencyPanelView: catalog has " + catalog.packs.Length +
                " packs but only " + items.Length + " card views. Add cards in the scene.");
        }
    }

    private CurrencyPackItemView[] ResolvePackItems()
    {
        if (packItemViews != null && packItemViews.Length > 0)
            return packItemViews;

        if (contentRoot != null)
            return contentRoot.GetComponentsInChildren<CurrencyPackItemView>(true);

        return null;
    }
}
