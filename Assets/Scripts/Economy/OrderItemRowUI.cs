// ──────────────────────────────────────────────
// TheSprouty | Economy/OrderItemRowUI.cs
// Displays a single order item in the Order scroll view.
// Handles +1 / -1 quantity. Destroys self when quantity reaches 0.
// ──────────────────────────────────────────────
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderItemRowUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private Image    itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Button       removeButton;
    [SerializeField] private SoundEventSO removeSound;

    // ----------------------------------------------------------
    // Properties
    // ----------------------------------------------------------
    public ShopItemEntry Entry    { get; private set; }
    public int           Quantity { get; private set; }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>Initializes this row with item data and starting quantity.</summary>
    public void Setup(ShopItemEntry entry, int initialQuantity = 1)
    {
        Entry    = entry;
        Quantity = initialQuantity;

        itemIcon.sprite   = entry.item.icon;
        itemNameText.text = entry.item.itemName;
        priceText.text    = $"Price: {entry.buyPrice}";

        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(OnRemoveClicked);

        RefreshUI();
    }

    /// <summary>Adds quantity from outside (e.g. ShopItemRow + button).</summary>
    public void AddQuantity(int amount = 1)
    {
        Quantity += amount;
        RefreshUI();
        BuyPageUI.Instance.RefreshTotal();
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void OnRemoveClicked()
    {
        removeSound?.Play();
        Quantity--;

        if (Quantity <= 0)
        {
            BuyPageUI.Instance.RefreshTotal();
            Destroy(gameObject);
            return;
        }

        RefreshUI();
        BuyPageUI.Instance.RefreshTotal();
    }

    private void RefreshUI()
    {
        quantityText.text = $"x{Quantity}";
    }
}
