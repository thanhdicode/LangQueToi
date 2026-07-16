// ──────────────────────────────────────────────
// TheSprouty | UI/SeedBagSlot.cs
// Extends SelectToolSlot for the SeedBag slot in tier 1 wheel.
// Equips the SeedBag tool, closes tier 1 and opens Seed Wheel (tier 2).
// ──────────────────────────────────────────────
using UnityEngine;

public class SeedBagSlot : SelectToolSlot
{
    [Header("Seed Wheel")]
    [SerializeField] private RadialSeedWheelUI radialSeedWheelUI;

    public override void SelectTool()
    {
        // base already equips the tool, highlights this slot, and closes the tool wheel.
        base.SelectTool();
        radialSeedWheelUI.Open();
    }
}
