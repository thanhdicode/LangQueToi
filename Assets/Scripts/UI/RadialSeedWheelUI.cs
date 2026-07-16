// ──────────────────────────────────────────────
// TheSprouty | UI/RadialSeedWheelUI.cs
// Manages the Seed Wheel (tier 2). Opened by SeedBagSlot, closed by TAB toggle.
// Listens to InventoryManager.OnInventoryChanged and refreshes slot quantities.
// ──────────────────────────────────────────────
using System;
using UnityEngine;

public class RadialSeedWheelUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [Header("References")]
    [SerializeField] private SelectSeedSlot[] seedSlots;
    [SerializeField] private RadialWheelAnimator wheelAnimator;
    [SerializeField] private PlayerIndicator playerIndicator;
    [SerializeField] private RadialToolWheelUI radialToolWheelUI;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private CanvasGroup _canvasGroup;

    public static bool IsOpen { get; private set; }

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        SetVisible(false);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
            RefreshAllSlots(); // initial sync
        }
        else
        {
            Debug.LogWarning("[RadialSeedWheelUI] InventoryManager.Instance is null at Start — slots won't refresh.");
        }
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------

    public void Open()
    {
        IsOpen = true;
        wheelAnimator.Show();
        playerIndicator.gameObject.SetActive(false);
    }

    public void Close()
    {
        IsOpen = false;
        wheelAnimator.Hide();
        playerIndicator.gameObject.SetActive(true);
    }

    public void OnSlotSelected(SelectSeedSlot selectedSlot)
    {
        foreach (SelectSeedSlot slot in seedSlots)
            slot.SetSelected(slot == selectedSlot);
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void HandleInventoryChanged(object sender, EventArgs e)
    {
        RefreshAllSlots();
        EnsureValidEquippedSeed();
    }

    private void RefreshAllSlots()
    {
        foreach (SelectSeedSlot slot in seedSlots)
            slot.RefreshFromInventory();
    }

    /// <summary>
    /// Keeps Player.EquippedSeed in sync with inventory while SeedBag is equipped.
    /// If the current seed runs out, picks the next slot with stock.
    /// If no seed remains anywhere, unequips the seed and switches the tool to None.
    /// No-op when player is not currently holding SeedBag.
    /// </summary>
    private void EnsureValidEquippedSeed()
    {
        if (Player.Instance == null) return;
        if (Player.Instance.EquippedToolType != ToolType.SeedBag) return;

        SeedSO equipped = Player.Instance.EquippedSeed;
        bool equippedHasStock = equipped != null
            && InventoryManager.Instance != null
            && InventoryManager.Instance.GetItemQuantity(equipped) > 0;

        if (equippedHasStock) return;

        SelectSeedSlot nextSlot = FindFirstSlotWithStock();
        if (nextSlot != null)
        {
            // Low-level equip — avoids triggering Close() on an already-closed wheel
            // (SelectSeed() is the user-click entry that also closes the wheel).
            Player.Instance.EquipSeed(nextSlot.Seed);
            OnSlotSelected(nextSlot);
        }
        else
        {
            Player.Instance.UnequipSeed();
            if (radialToolWheelUI != null)
                radialToolWheelUI.SelectByToolType(ToolType.None);
        }
    }

    private SelectSeedSlot FindFirstSlotWithStock()
    {
        foreach (SelectSeedSlot slot in seedSlots)
        {
            if (slot.Quantity > 0) return slot;
        }
        return null;
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1 : 0;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
