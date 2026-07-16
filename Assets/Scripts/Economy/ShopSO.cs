// ──────────────────────────────────────────────
// TheSprouty | Economy/ShopSO.cs
// ScriptableObject defining items available for purchase in a shop.
// ──────────────────────────────────────────────
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shop", menuName = "TheSprouty/Economy/Shop")]
public class ShopSO : ScriptableObject
{
    [Header("Shop Info")]
    public string shopName;

    [Header("Items for Sale")]
    public List<ShopItemEntry> items;
}

[Serializable]
public class ShopItemEntry
{
    public ItemSO item;
    public int buyPrice;
}
