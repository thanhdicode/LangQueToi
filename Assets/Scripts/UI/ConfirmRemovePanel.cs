// ──────────────────────────────────────────────
// TheSprouty | UI/ConfirmRemovePanel.cs
// Confirm dialog before deleting a save slot.
// ──────────────────────────────────────────────
using UnityEngine;
using UnityEngine.UI;

public class ConfirmRemovePanel : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private Button           confirmButton;
    [SerializeField] private Button           cancelButton;
    [SerializeField] private PopScaleAnimator animator;

    [Header("SFX")]
    [SerializeField] private SoundEventSO sfxConfirm;
    [SerializeField] private SoundEventSO sfxCancel;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private int              _targetSlotIndex;
    private RemoveSaveButton _callerButton;
    private SavePanel        _savePanel;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
    }

    private void OnDestroy()
    {
        confirmButton.onClick.RemoveListener(OnConfirm);
        cancelButton.onClick.RemoveListener(OnCancel);
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>
    /// Opens the confirm panel for the given slot.
    /// Called by RemoveSaveButton.
    /// </summary>
    public void Open(int slotIndex, RemoveSaveButton caller, SavePanel savePanel)
    {
        _targetSlotIndex = slotIndex;
        _callerButton    = caller;
        _savePanel       = savePanel;
        animator.Show();
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void OnConfirm()
    {
        sfxConfirm?.Play();
        SaveManager.DeleteSlot(_targetSlotIndex);
        _savePanel.ReplaceWithEmptySlot(_targetSlotIndex);
        _callerButton = null;
        animator.Hide();
    }

    private void OnCancel()
    {
        sfxCancel?.Play();
        if (_callerButton != null)
            _callerButton.Deactivate();

        _callerButton = null;
        animator.Hide();
    }
}
