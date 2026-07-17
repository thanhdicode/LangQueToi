// ──────────────────────────────────────────────
// TheSprouty | Economy/MyItemsRowUI.cs
// Displays a single sellable inventory item in the Sell page.
// Arrow button adds item to the ToSell list.
// ──────────────────────────────────────────────
using LangQueToi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MyItemsRowUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private Image    itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button       addButton;
    [SerializeField] private SoundEventSO addSound;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private ItemSO _item;

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>Populates this row with inventory item data.</summary>
    public void Setup(ItemSO item, int quantity)
    {
        _item = item;

        itemIcon.sprite   = item.icon;
        itemNameText.text = item.itemName;
        priceText.text    = Loc.Format("shop.sell_stock", Loc.Gold(item.sellValue), quantity);

        addButton.onClick.RemoveAllListeners();
        addButton.onClick.AddListener(OnAddClicked);
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void OnAddClicked()
    {
        addSound?.Play();
        SellPageUI.Instance.AddToSell(_item, 1);
    }
}
