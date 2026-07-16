// ──────────────────────────────────────────────
// TheSprouty | Economy/ShopItemRowUI.cs
// Displays a single ShopItemEntry in the Shop scroll view.
// ──────────────────────────────────────────────
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemRowUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private Image     itemIcon;
    [SerializeField] private TMP_Text  itemNameText;
    [SerializeField] private TMP_Text  priceText;
    [SerializeField] private Button       addButton;
    [SerializeField] private SoundEventSO addSound;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private ShopItemEntry _entry;

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>Populates this row with the given ShopItemEntry data.</summary>
    public void Setup(ShopItemEntry entry)
    {
        _entry = entry;

        itemIcon.sprite  = entry.item.icon;
        itemNameText.text = entry.item.itemName;
        priceText.text    = $"Price: {entry.buyPrice}";

        addButton.onClick.RemoveAllListeners();
        addButton.onClick.AddListener(OnAddClicked);
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void OnAddClicked()
    {
        addSound?.Play();
        BuyPageUI.Instance.AddToOrder(_entry);
    }
}
