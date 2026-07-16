// ──────────────────────────────────────────────
// TheSprouty | UI/SelectSeedSlot.cs
// Handles seed selection in the Seed Wheel (tier 2).
// Reflects inventory quantity: shows count, greys out + disables when out of stock.
// ──────────────────────────────────────────────
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SelectSeedSlot : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [Header("Data")]
    [SerializeField] private SeedSO seed;

    [Header("Visuals")]
    [SerializeField] private GameObject slotVisual;
    [SerializeField] private GameObject slotSelectedVisual;
    [SerializeField] private GameObject greyOutVisual;
    [SerializeField] private TMP_Text quantityText;

    [Header("Interaction")]
    [SerializeField] private RadialSeedWheelUI radialSeedWheelUI;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private Button _button;
    private bool _isSelected;
    private int _quantity;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    // ----------------------------------------------------------
    // Properties
    // ----------------------------------------------------------
    public SeedSO Seed => seed;
    public int Quantity => _quantity;

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------

    /// <summary>Called by Button.onClick to equip this seed.</summary>
    public void SelectSeed()
    {
        // Defensive guard — button should already be disabled when quantity = 0
        if (_quantity <= 0) return;

        Player.Instance.EquipSeed(seed);
        radialSeedWheelUI.OnSlotSelected(this);
        radialSeedWheelUI.Close();
    }

    /// <summary>
    /// Called by RadialSeedWheelUI when the player picks a slot.
    /// Stores selection state and refreshes visuals.
    /// </summary>
    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        UpdateVisuals();
    }

    /// <summary>
    /// Reads the current quantity of this seed from the inventory
    /// and updates visuals + interactable state. Safe to call repeatedly.
    /// </summary>
    public void RefreshFromInventory()
    {
        _quantity = (InventoryManager.Instance != null && seed != null)
            ? InventoryManager.Instance.GetItemQuantity(seed)
            : 0;

        // Out-of-stock slot can't stay highlighted as the active selection.
        if (_quantity <= 0)
            _isSelected = false;

        UpdateVisuals();
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void UpdateVisuals()
    {
        bool hasStock = _quantity > 0;

        greyOutVisual.SetActive(!hasStock);
        slotVisual.SetActive(hasStock && !_isSelected);
        slotSelectedVisual.SetActive(hasStock && _isSelected);

        quantityText.gameObject.SetActive(hasStock);
        if (hasStock)
            quantityText.text = _quantity.ToString();

        _button.interactable = hasStock;
    }
}
