// ──────────────────────────────────────────────
// TheSprouty | UI/RemoveSaveButton.cs
// Handles the remove button on a FilledSaveSlot.
// Click: enable outline + open ConfirmRemovePanel.
// ──────────────────────────────────────────────
using UnityEngine;
using UnityEngine.UI;

public class RemoveSaveButton : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private SoundEventSO sfxClick;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private Outline            _outline;
    private Button             _button;
    private int                _slotIndex;
    private ConfirmRemovePanel _confirmPanel;
    private SavePanel          _savePanel;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        _outline = GetComponent<Outline>();
        _button  = GetComponent<Button>();

        if (_outline != null)
            _outline.enabled = false;

        _button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveListener(OnClick);
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>
    /// Called by FilledSaveSlotUI after instantiation.
    /// </summary>
    public void Init(int slotIndex, ConfirmRemovePanel confirmPanel, SavePanel savePanel)
    {
        _slotIndex    = slotIndex;
        _confirmPanel = confirmPanel;
        _savePanel    = savePanel;
    }

    /// <summary>
    /// Disables outline. Called by ConfirmRemovePanel on cancel.
    /// </summary>
    public void Deactivate()
    {
        if (_outline != null)
            _outline.enabled = false;
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void OnClick()
    {
        sfxClick?.Play();

        if (_outline != null)
            _outline.enabled = true;

        _confirmPanel.Open(_slotIndex, this, _savePanel);
    }
}
