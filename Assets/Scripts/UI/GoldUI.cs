// ──────────────────────────────────────────────
// TheSprouty | UI/GoldUI.cs
// Listens to EconomyManager.OnGoldChanged and updates the gold display.
// Attach on the GoldText GameObject.
// ──────────────────────────────────────────────
using System;
using LangQueToi;
using TMPro;
using UnityEngine;

public class GoldUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private TMP_Text goldText;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Start()
    {
        EconomyManager.Instance.OnGoldChanged += OnGoldChanged;
        Refresh();
    }

    private void OnDestroy()
    {
        EconomyManager.Instance.OnGoldChanged -= OnGoldChanged;
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void OnGoldChanged(object sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        goldText.text = Loc.Gold(EconomyManager.Instance.Gold);
    }
}
