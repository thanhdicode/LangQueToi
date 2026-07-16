// ──────────────────────────────────────────────
// TheSprouty | NPC/Base/NPCShop.cs
// Shop NPC: detects player proximity and opens DialoguePanel on TalkToNPC press.
// ──────────────────────────────────────────────
using System;
using UnityEngine;

public class NPCShop : BaseTriggerZone
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [Header("References")]
    [SerializeField] private GameObject interactIndicator;
    [SerializeField] private GameInput gameInput;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private bool _playerInRange;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Update()
    {
        if (!_playerInRange) return;

        bool anyUIOpen = DialoguePanelUI.Instance.IsOpen
                      || ShopPanelUI.IsOpen
                      || InventoryUI.IsOpen
                      || RadialToolWheelUI.IsOpen;
        interactIndicator.SetActive(!anyUIOpen);
    }

    // ----------------------------------------------------------
    // Protected hooks
    // ----------------------------------------------------------
    protected override void OnPlayerEnter()
    {
        _playerInRange = true;
        interactIndicator.SetActive(true);

        if (gameInput != null)
            gameInput.OnTalkToNPCAction += HandleTalkToNPC;
    }

    protected override void OnPlayerExit()
    {
        _playerInRange = false;
        interactIndicator.SetActive(false);

        if (gameInput != null)
            gameInput.OnTalkToNPCAction -= HandleTalkToNPC;
    }

    // ----------------------------------------------------------
    // Private event handlers
    // ----------------------------------------------------------
    private void HandleTalkToNPC(object sender, EventArgs e)
    {
        // Defensive guards — block dialogue while other UIs are open
        if (InventoryUI.IsOpen)        return;
        if (RadialToolWheelUI.IsOpen)  return;
        if (Player.Instance.IsInDialogue) return;

        DialoguePanelUI.Instance.Open();
    }
}
