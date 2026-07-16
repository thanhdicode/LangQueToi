// ──────────────────────────────────────────────
// TheSprouty | UI/FilledSaveSlotUI.cs
// Extends SaveSlotUI for slots that have existing save data.
// Enables CharacterModel Animator on select, populates Day and Gold text.
// ──────────────────────────────────────────────
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FilledSaveSlotUI : SaveSlotUI
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private Animator        characterAnimator;
    [SerializeField] private Image           characterImage;
    [SerializeField] private TMP_Text        dayText;
    [SerializeField] private TMP_Text        goldText;
    [SerializeField] private RemoveSaveButton removeSaveButton;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private Sprite _defaultSprite;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Start()
    {
        _defaultSprite            = characterImage.sprite;
        characterAnimator.enabled = false;
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>
    /// Initializes slot index and populates UI from save data.
    /// Called by SavePanel after instantiation.
    /// </summary>
    public override void Init(int slotIndex)
    {
        base.Init(slotIndex);
        PopulateFromSave(slotIndex);
    }

    /// <summary>
    /// Injects ConfirmRemovePanel and SavePanel references into RemoveSaveButton.
    /// Called by SavePanel after instantiation.
    /// </summary>
    public void InitRemoveButton(ConfirmRemovePanel confirmPanel, SavePanel savePanel)
    {
        removeSaveButton.Init(SlotIndex, confirmPanel, savePanel);
    }

    // ----------------------------------------------------------
    // Protected hooks
    // ----------------------------------------------------------
    protected override void OnSelected()
    {
        base.OnSelected(); // plays sfxSelect
        characterAnimator.enabled = true;
    }

    protected override void OnDeselected()
    {
        characterAnimator.enabled = false;
        characterImage.sprite     = _defaultSprite;
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void PopulateFromSave(int slotIndex)
    {
        GameSaveData data = SaveManager.GetSlotData(slotIndex);
        if (data == null) return;

        dayText.text  = $"{data.time.currentDay} Days";
        goldText.text = data.gold.ToString();
    }
}
