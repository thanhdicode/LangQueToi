// ──────────────────────────────────────────────
// TheSprouty | Scripts/Save/ItemRegistrySO.cs
// Central registry of all ScriptableObject assets that need to be
// referenced by name in save data.
//
// WHY: JsonUtility saves strings, not SO references.
//      On load, SaveManager calls GetItem("WoodLog") to get back the
//      actual ItemSO asset — no Resources folder required.
//
// HOW TO USE:
//   1. Create one asset: Right-click → TheSprouty/Save/Item Registry
//   2. Assign it to SaveManager in the Inspector.
//   3. Drag every ItemSO, SeedSO, and CropDataSO asset into the lists.
//   4. Run ValidateRegistry() from the context menu to catch duplicates.
//
// KEY: ScriptableObject.name (= asset filename, e.g. "WoodLog")
//      NOT the itemName display field, which users can rename freely.
// ──────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemRegistry", menuName = "TheSprouty/Save/Item Registry")]
public class ItemRegistrySO : ScriptableObject
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------

    [Header("Items  (ItemSO + SeedSO — drag all item assets here)")]
    [SerializeField] private List<ItemSO> allItems = new List<ItemSO>();

    [Header("Crops  (CropDataSO — drag all crop data assets here)")]
    [SerializeField] private List<CropDataSO> allCrops = new List<CropDataSO>();

    // ----------------------------------------------------------
    // Public lookup API
    // ----------------------------------------------------------

    /// <summary>
    /// Returns the ItemSO whose asset name matches <paramref name="soName"/>.
    /// SeedSO is a subclass of ItemSO — cast the result if needed.
    /// Returns null and logs a warning if not found.
    /// </summary>
    public ItemSO GetItem(string soName)
    {
        if (string.IsNullOrEmpty(soName)) return null;

        foreach (ItemSO item in allItems)
        {
            if (item != null && item.name == soName)
                return item;
        }

        Debug.LogWarning($"[ItemRegistry] Item not found: \"{soName}\". " +
                         "Did you forget to add it to the registry?");
        return null;
    }

    /// <summary>
    /// Returns the CropDataSO whose asset name matches <paramref name="soName"/>.
    /// Returns null and logs a warning if not found.
    /// </summary>
    public CropDataSO GetCrop(string soName)
    {
        if (string.IsNullOrEmpty(soName)) return null;

        foreach (CropDataSO crop in allCrops)
        {
            if (crop != null && crop.name == soName)
                return crop;
        }

        Debug.LogWarning($"[ItemRegistry] Crop not found: \"{soName}\". " +
                         "Did you forget to add it to the registry?");
        return null;
    }

    // ----------------------------------------------------------
    // Editor validation
    // ----------------------------------------------------------

#if UNITY_EDITOR
    /// <summary>
    /// Call from the context menu to detect duplicate or missing entries.
    /// </summary>
    [ContextMenu("Validate Registry")]
    private void ValidateRegistry()
    {
        bool ok = true;

        ok &= CheckDuplicates("Items",  allItems);
        ok &= CheckDuplicates("Crops",  allCrops);

        if (ok)
            Debug.Log("[ItemRegistry] Validation passed — no issues found.");
    }

    private bool CheckDuplicates<T>(string label, List<T> list) where T : ScriptableObject
    {
        bool ok = true;
        HashSet<string> seen = new HashSet<string>();

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null)
            {
                Debug.LogWarning($"[ItemRegistry] {label}[{i}] is null.");
                ok = false;
                continue;
            }

            string key = list[i].name;
            if (!seen.Add(key))
            {
                Debug.LogWarning($"[ItemRegistry] Duplicate in {label}: \"{key}\".");
                ok = false;
            }
        }

        return ok;
    }
#endif
}
