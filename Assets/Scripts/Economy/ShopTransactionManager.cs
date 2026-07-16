// ──────────────────────────────────────────────
// TheSprouty | Economy/ShopTransactionManager.cs
// Resolves pending buy and sell orders when a new day starts.
// Sell → Buy order (sell first to gain gold before checking buy affordability).
// Notifications shown on OnSleepEnded: sell first, then buy summary after delay.
// ──────────────────────────────────────────────
using System;
using System.Collections;
using UnityEngine;

public class ShopTransactionManager : MonoBehaviour
{
    // ----------------------------------------------------------
    // Singleton
    // ----------------------------------------------------------
    public static ShopTransactionManager Instance { get; private set; }

    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [Tooltip("Delay before showing buy notification after sell notification.")]
    [SerializeField] private float buyNotificationDelay = 3.5f;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private int _pendingSellGold;
    private int _pendingBuyGoldSpent;
    private int _pendingBuySkipped;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        DayCycleManager.Instance.OnDayPassed  += OnDayPassed;
        DayCycleManager.Instance.OnSleepEnded += OnSleepEnded;
    }

    private void OnDestroy()
    {
        if (DayCycleManager.Instance == null) return;
        DayCycleManager.Instance.OnDayPassed  -= OnDayPassed;
        DayCycleManager.Instance.OnSleepEnded -= OnSleepEnded;
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void OnDayPassed(object sender, int day)
    {
        ResolveSellOrders();
        ResolveBuyOrders();
    }

    private void OnSleepEnded(object sender, EventArgs e)
    {
        StartCoroutine(ShowNotificationsRoutine());
    }

    private IEnumerator ShowNotificationsRoutine()
    {
        if (_pendingSellGold > 0)
        {
            NotificationManager.Instance?.ShowMessage($"Clove paid you {_pendingSellGold} Gold!");
            _pendingSellGold = 0;
            yield return new WaitForSeconds(buyNotificationDelay);
        }

        if (_pendingBuyGoldSpent > 0 || _pendingBuySkipped > 0)
        {
            string msg = _pendingBuySkipped > 0
                ? $"Partial order: spent {_pendingBuyGoldSpent} Gold, {_pendingBuySkipped} item(s) skipped"
                : $"Order delivered! Spent {_pendingBuyGoldSpent} Gold";

            NotificationManager.Instance?.ShowMessage(msg);
            _pendingBuyGoldSpent = 0;
            _pendingBuySkipped   = 0;
        }
    }

    private void ResolveBuyOrders()
    {
        if (BuyPageUI.Instance == null) return;

        OrderItemRowUI[] orders = BuyPageUI.Instance.GetOrderRows();
        if (orders.Length == 0) return;

        foreach (OrderItemRowUI row in orders)
        {
            int totalCost = row.Entry.buyPrice * row.Quantity;

            if (!EconomyManager.Instance.CanAfford(totalCost))
            {
                _pendingBuySkipped++;
                continue;
            }

            EconomyManager.Instance.SpendGold(totalCost);
            InventoryManager.Instance.AddItem(row.Entry.item, row.Quantity);
            _pendingBuyGoldSpent += totalCost;
            Destroy(row.gameObject);
        }

        BuyPageUI.Instance.RefreshTotal();
    }

    private void ResolveSellOrders()
    {
        if (SellPageUI.Instance == null) return;

        ToSellRowUI[] rows = SellPageUI.Instance.GetToSellRows();
        if (rows.Length == 0) return;

        foreach (ToSellRowUI row in rows)
        {
            int earned = row.Item.sellValue * row.Quantity;
            EconomyManager.Instance.AddGold(earned);
            _pendingSellGold += earned;
            Destroy(row.gameObject);
        }

        SellPageUI.Instance.RefreshTotal();
    }
}
