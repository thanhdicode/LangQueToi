// ──────────────────────────────────────────────
// TheSprouty | Economy/SellPageUI.cs
// Manages the Sell page: populates sellable inventory items,
// handles to-sell list and total gold display.
// Attach on the SellPage GameObject.
// ──────────────────────────────────────────────
using System;
using LangQueToi;
using TMPro;
using UnityEngine;

public class SellPageUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Singleton
    // ----------------------------------------------------------
    public static SellPageUI Instance { get; private set; }

    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [Header("My Items")]
    [SerializeField] private Transform  myItemsContent;
    [SerializeField] private GameObject myItemsRowPrefab;

    [Header("To Sell")]
    [SerializeField] private Transform  toSellContent;
    [SerializeField] private GameObject toSellRowPrefab;
    [SerializeField] private TMP_Text   totalText;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private bool _subscribed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= OnInventoryChanged;
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>Populates sellable items from inventory. Called by ShopPanelUI on Open.</summary>
    public void Initialize()
    {
        if (!_subscribed)
        {
            InventoryManager.Instance.OnInventoryChanged += OnInventoryChanged;
            _subscribed = true;
        }
        PopulateMyItems();
        RefreshTotal();
    }

    /// <summary>Removes quantity from inventory and adds to ToSell list.
    /// Called by MyItemsRowUI (qty=1) and ItemActionPanelUI.SellAll (qty=all).</summary>
    public void AddToSell(ItemSO item, int quantity)
    {
        if (quantity <= 0) return;

        InventoryManager.Instance.RemoveItem(item, quantity);

        foreach (Transform child in toSellContent)
        {
            ToSellRowUI existing = child.GetComponent<ToSellRowUI>();
            if (existing != null && existing.Item == item)
            {
                existing.AddQuantity(quantity);
                return;
            }
        }

        GameObject row = Instantiate(toSellRowPrefab, toSellContent);
        row.GetComponent<ToSellRowUI>().Setup(item, quantity);
        RefreshTotal();
    }

    /// <summary>Recalculates and updates the total gold to receive.</summary>
    public void RefreshTotal()
    {
        int total = 0;
        foreach (Transform child in toSellContent)
        {
            ToSellRowUI row = child.GetComponent<ToSellRowUI>();
            if (row != null)
                total += row.Item.sellValue * row.Quantity;
        }
        totalText.text = Loc.Format("shop.total", Loc.Gold(total));
    }

    /// <summary>Returns all current to-sell rows. Used by ShopTransactionManager.</summary>
    public ToSellRowUI[] GetToSellRows() => toSellContent.GetComponentsInChildren<ToSellRowUI>();

    /// <summary>Clears the to-sell list. Called after items are sold.</summary>
    public void ClearToSell()
    {
        foreach (Transform child in toSellContent)
            Destroy(child.gameObject);
        RefreshTotal();
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void PopulateMyItems()
    {
        foreach (Transform child in myItemsContent)
            Destroy(child.gameObject);

        foreach (ItemSlot slot in InventoryManager.Instance.GetSlots())
        {
            if (slot.IsEmpty) continue;
            if (slot.GetItemSO().sellValue <= 0) continue;

            GameObject row = Instantiate(myItemsRowPrefab, myItemsContent);
            row.GetComponent<MyItemsRowUI>().Setup(slot.GetItemSO(), slot.GetQuantity());
        }
    }

    private void OnInventoryChanged(object sender, EventArgs e)
    {
        PopulateMyItems();
    }
}
