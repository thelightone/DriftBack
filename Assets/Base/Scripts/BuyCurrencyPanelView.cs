using System.Collections.Generic;
using UnityEngine;

public class BuyCurrencyPanelView : MonoBehaviour
{
    public Transform contentRoot;
    public CurrencyPackItemView itemPrefab;

    private readonly List<CurrencyPackItemView> _spawnedItems = new();

    public void Rebuild(CurrencyPackCatalog catalog, System.Action<CurrencyPackDefinition> onBuy)
    {
        Debug.Log("BuyCurrencyPanelView.Rebuild called");

        Clear();

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

        if (contentRoot == null)
        {
            Debug.LogError("BuyCurrencyPanelView: contentRoot is NULL");
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogError("BuyCurrencyPanelView: itemPrefab is NULL");
            return;
        }

        Debug.Log("BuyCurrencyPanelView: packs count = " + catalog.packs.Length);

        for (int i = 0; i < catalog.packs.Length; i++)
        {
            var pack = catalog.packs[i];
            if (pack == null)
            {
                Debug.LogWarning("BuyCurrencyPanelView: pack[" + i + "] is NULL");
                continue;
            }

            Debug.Log("Spawn currency pack item: " + pack.productId + ", name=" + pack.displayName);

            var item = Instantiate(itemPrefab, contentRoot);
            item.Setup(pack, onBuy);
            _spawnedItems.Add(item);
        }

        Debug.Log("BuyCurrencyPanelView: spawned items = " + _spawnedItems.Count);
    }

    private void Clear()
    {
        Debug.Log("BuyCurrencyPanelView.Clear called. Old items count = " + _spawnedItems.Count);

        for (int i = 0; i < _spawnedItems.Count; i++)
        {
            if (_spawnedItems[i] != null)
                Destroy(_spawnedItems[i].gameObject);
        }

        _spawnedItems.Clear();
    }
}