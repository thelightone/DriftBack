using UnityEngine;

[CreateAssetMenu(fileName = "CurrencyPackDefinition", menuName = "Game/Currency Pack Definition")]
public class CurrencyPackDefinition : ScriptableObject
{
    public string productId;
    public string displayName;
    public int softCurrencyAmount;
    public int starsPrice;
    public Sprite icon;
}