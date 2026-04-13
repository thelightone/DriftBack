using UnityEngine;

[CreateAssetMenu(fileName = "CurrencyPackCatalog", menuName = "Game/Currency Pack Catalog")]
public class CurrencyPackCatalog : ScriptableObject
{
    public CurrencyPackDefinition[] packs;
}