// ──────────────────────────────────────────────
// TheSprouty | Economy/ToSellRowUI.cs
// Displays a single item in the To Sell list.
// Minus button decrements quantity. Destroys self when quantity reaches 0.
// ──────────────────────────────────────────────
using LangQueToi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToSellRowUI : MonoBehaviour
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
    public ItemSO Item     { get; private set; }
    public int    Quantity { get; private set; }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>Initializes this row with item data and starting quantity.</summary>
    public void Setup(ItemSO item, int initialQuantity = 1)
    {
        Item     = item;
        Quantity = initialQuantity;

        itemIcon.sprite   = item.icon;
        itemNameText.text = item.itemName;
        priceText.text    = Loc.Format("shop.sell_unit", Loc.Gold(item.sellValue));

        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(OnRemoveClicked);

        RefreshUI();
    }

    /// <summary>Increments quantity from outside (e.g. MyItemsRow arrow button).</summary>
    public void AddQuantity(int amount = 1)
    {
        Quantity += amount;
        RefreshUI();
        SellPageUI.Instance.RefreshTotal();
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void OnRemoveClicked()
    {
        removeSound?.Play();
        InventoryManager.Instance.AddItem(Item, 1);
        Quantity--;

        if (Quantity <= 0)
        {
            SellPageUI.Instance.RefreshTotal();
            Destroy(gameObject);
            return;
        }

        RefreshUI();
        SellPageUI.Instance.RefreshTotal();
    }

    private void RefreshUI()
    {
        quantityText.text = $"x{Quantity}";
    }
}
