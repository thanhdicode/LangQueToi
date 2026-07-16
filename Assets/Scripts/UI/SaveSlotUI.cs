// ──────────────────────────────────────────────
// TheSprouty | UI/SaveSlotUI.cs
// Handles save slot selection logic.
// Click 1: select (enable outline).
// Click 2: confirm (load game / start new game).
// Selecting another slot deselects the current one.
// ──────────────────────────────────────────────
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private Outline outline;

    [Header("SFX")]
    [SerializeField] private SoundEventSO sfxSelect;
    [SerializeField] private SoundEventSO sfxConfirm;

    // ----------------------------------------------------------
    // Properties
    // ----------------------------------------------------------
    public int SlotIndex { get; private set; }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>
    /// Called by SavePanel after instantiation to assign slot index.
    /// </summary>
    public virtual void Init(int slotIndex)
    {
        SlotIndex = slotIndex;
    }

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private static SaveSlotUI _currentSelected;
    private bool _isSelected;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void OnDestroy()
    {
        if (_currentSelected == this)
            _currentSelected = null;
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>
    /// Called by Button OnClick. Handles select and confirm.
    /// </summary>
    public void OnClick()
    {
        if (!_isSelected)
        {
            Select();
        }
        else
        {
            Confirm();
        }
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void Select()
    {
        if (_currentSelected != null && _currentSelected != this)
            _currentSelected.Deselect();

        _isSelected = true;
        _currentSelected = this;
        outline.enabled = true;

        OnSelected();
    }

    private void Deselect()
    {
        _isSelected = false;
        outline.enabled = false;

        OnDeselected();
    }

    private void Confirm()
    {
        OnConfirmed();
    }

    // ----------------------------------------------------------
    // Protected hooks (override in subclass if needed)
    // ----------------------------------------------------------
    /// <summary> Called when this slot is selected (click 1). </summary>
    protected virtual void OnSelected()
    {
        sfxSelect?.Play();
    }

    /// <summary> Called when this slot is deselected. </summary>
    protected virtual void OnDeselected()
    {
    }

    /// <summary> Called when this slot is confirmed (click 2). </summary>
    protected virtual void OnConfirmed()
    {
        sfxConfirm?.Play();
        SaveManager.SetSlot(SlotIndex);
        SceneTransitionManager.TransitionTo("MainScene");
    }
}
