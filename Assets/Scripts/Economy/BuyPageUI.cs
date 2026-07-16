// ──────────────────────────────────────────────
// TheSprouty | Economy/BuyPageUI.cs
// Manages the Buy page: populates shop items, handles order list and total.
// Attach on the BuyPage GameObject.
// ──────────────────────────────────────────────
using TMPro;
using UnityEngine;

public class BuyPageUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Singleton
    // ----------------------------------------------------------
    public static BuyPageUI Instance { get; private set; }

    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [Header("Shop")]
    [SerializeField] private ShopSO     shopSO;
    [SerializeField] private Transform  shopContent;
    [SerializeField] private GameObject shopItemRowPrefab;

    [Header("Order")]
    [SerializeField] private Transform  orderContent;
    [SerializeField] private GameObject orderItemRowPrefab;
    [SerializeField] private TMP_Text   totalText;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>Populates shop list and resets order. Called by ShopPanelUI on Open.</summary>
    public void Initialize()
    {
        PopulateShopItems();
        RefreshTotal();
    }

    /// <summary>Clears the order list. Called after day passes and items are delivered.</summary>
    public void ClearOrder()
    {
        foreach (Transform child in orderContent)
            Destroy(child.gameObject);
        RefreshTotal();
    }

    /// <summary>Returns all current order rows. Used by ShopTransactionManager on day passed.</summary>
    public OrderItemRowUI[] GetOrderRows() => orderContent.GetComponentsInChildren<OrderItemRowUI>();

    /// <summary>Adds item to order or increments quantity if already exists.</summary>
    public void AddToOrder(ShopItemEntry entry)
    {
        foreach (Transform child in orderContent)
        {
            OrderItemRowUI existing = child.GetComponent<OrderItemRowUI>();
            if (existing != null && existing.Entry == entry)
            {
                existing.AddQuantity(1);
                return;
            }
        }

        GameObject row = Instantiate(orderItemRowPrefab, orderContent);
        row.GetComponent<OrderItemRowUI>().Setup(entry);
        RefreshTotal();
    }

    /// <summary>Recalculates and updates the total price display.</summary>
    public void RefreshTotal()
    {
        int total = 0;
        foreach (Transform child in orderContent)
        {
            OrderItemRowUI row = child.GetComponent<OrderItemRowUI>();
            if (row != null)
                total += row.Entry.buyPrice * row.Quantity;
        }
        totalText.text = $"TOTAL: {total}";
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void PopulateShopItems()
    {
        foreach (Transform child in shopContent)
            Destroy(child.gameObject);

        foreach (ShopItemEntry entry in shopSO.items)
        {
            GameObject row = Instantiate(shopItemRowPrefab, shopContent);
            row.GetComponent<ShopItemRowUI>().Setup(entry);
        }
    }

}
