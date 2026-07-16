// ──────────────────────────────────────────────
// TheSprouty | Scripts/WorldItem.cs
// Represents a physical item lying in the world.
// Implements ICollectable so ItemPickup can trigger collection.
// Auto-despawns after a configurable number of in-game days.
// ──────────────────────────────────────────────
using System;
using UnityEngine;

public class WorldItem : MonoBehaviour, ICollectable
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private ItemSO itemSO;

    [Header("Despawn")]
    [Tooltip("In-game days before this item despawns if not collected. 0 = never despawn.")]
    [SerializeField] private int despawnDays = 3;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private int _daysRemaining;

    // ----------------------------------------------------------
    // Properties
    // ----------------------------------------------------------
    public string ObjectName    => itemSO != null ? itemSO.itemName : gameObject.name;
    public int    DaysRemaining => _daysRemaining;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        // Initialize here so SetDaysRemaining() called by SaveManager
        // (between Awake and Start) correctly overrides this value.
        _daysRemaining = despawnDays;
    }

    private void Start()
    {
        // Do NOT reset _daysRemaining here — Awake() handles default init,
        // SaveManager may have already called SetDaysRemaining() before Start() runs.
        if (DayCycleManager.Instance != null)
            DayCycleManager.Instance.OnDayPassed += HandleDayPassed;
    }

    private void OnDestroy()
    {
        if (DayCycleManager.Instance != null)
            DayCycleManager.Instance.OnDayPassed -= HandleDayPassed;
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    public ItemSO GetItemSO() => itemSO;

    /// <summary>
    /// Restores the despawn counter from save data.
    /// Called by SaveManager immediately after instantiating the item on load.
    /// </summary>
    public void SetDaysRemaining(int days)
    {
        _daysRemaining = days;
    }

    /// <summary>Called by ItemPickup when the player collects this item.</summary>
    public void OnCollected()
    {
        if (itemSO != null)
        {
            bool added = InventoryManager.Instance.AddItem(itemSO, 1);
            if (added)
                NotificationManager.Instance.ShowItemNotification(itemSO, 1);
        }
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void HandleDayPassed(object sender, int day)
    {
        if (despawnDays <= 0) return; // despawn disabled

        _daysRemaining--;
        if (_daysRemaining <= 0)
            Destroy(gameObject);
    }
}
