// ──────────────────────────────────────────────
// TheSprouty | UI/SavePanel.cs
// Instantiates save slot prefabs into the ScrollView Content.
// Checks each slot index — spawns FilledSaveSlot if save exists,
// EmptySaveSlot otherwise.
// ──────────────────────────────────────────────
using UnityEngine;

public class SavePanel : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private Transform         content;
    [SerializeField] private GameObject        emptySaveSlotPrefab;
    [SerializeField] private GameObject        filledSaveSlotPrefab;
    [SerializeField] private ConfirmRemovePanel confirmRemovePanel;
    [SerializeField] [Range(1, 10)] private int slotCount = 5;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private GameObject[] _slots;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Start()
    {
        _slots = new GameObject[slotCount];
        PopulateSlots();
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>
    /// Replaces a FilledSaveSlot with an EmptySaveSlot at the same position.
    /// Called by ConfirmRemovePanel after deletion.
    /// </summary>
    public void ReplaceWithEmptySlot(int slotIndex)
    {
        int siblingIndex = _slots[slotIndex].transform.GetSiblingIndex();

        Destroy(_slots[slotIndex]);
        SpawnSlot(slotIndex, emptySaveSlotPrefab);

        _slots[slotIndex].transform.SetSiblingIndex(siblingIndex);
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void PopulateSlots()
    {
        for (int i = 0; i < slotCount; i++)
        {
            GameObject prefab = SaveManager.GetSlotExists(i)
                ? filledSaveSlotPrefab
                : emptySaveSlotPrefab;

            SpawnSlot(i, prefab);
        }
    }

    private void SpawnSlot(int slotIndex, GameObject prefab)
    {
        GameObject slot = Instantiate(prefab, content);
        _slots[slotIndex] = slot;

        SaveSlotUI slotUI = slot.GetComponentInChildren<SaveSlotUI>();
        if (slotUI != null)
            slotUI.Init(slotIndex);

        if (slot.GetComponentInChildren<FilledSaveSlotUI>() is FilledSaveSlotUI filledUI)
            filledUI.InitRemoveButton(confirmRemovePanel, this);
    }
}
