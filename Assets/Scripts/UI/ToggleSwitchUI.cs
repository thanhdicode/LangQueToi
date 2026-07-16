// ──────────────────────────────────────────────
// TheSprouty | UI/ToggleSwitchUI.cs
// Custom sliding toggle — swaps sprites and moves
// the knob button between ON/OFF positions.
// ──────────────────────────────────────────────
using System;
using UnityEngine;
using UnityEngine.UI;

public class ToggleSwitchUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [Header("References")]
    [SerializeField] private Button         button;
    [SerializeField] private Image          buttonImage;
    [SerializeField] private Image          backgroundImage;

    [Header("Positions")]
    [SerializeField] private Vector2        buttonPosOn;
    [SerializeField] private Vector2        buttonPosOff;

    [Header("Sprites")]
    [SerializeField] private Sprite         buttonSpriteOn;
    [SerializeField] private Sprite         buttonSpriteOff;
    [SerializeField] private Sprite         backgroundSpriteOn;
    [SerializeField] private Sprite         backgroundSpriteOff;

    [Header("State")]
    [SerializeField] private bool           isOn = true;

    // ----------------------------------------------------------
    // Properties
    // ----------------------------------------------------------
    // Events
    // ----------------------------------------------------------
    public event Action<bool> OnToggled;

    // ----------------------------------------------------------
    // Properties
    // ----------------------------------------------------------
    public bool IsOn => isOn;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        button.onClick.AddListener(OnClick);
    }

    private void Start()
    {
        Refresh(playSfx: false);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnClick);
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>Set toggle state from code without firing OnClick.</summary>
    public void SetState(bool on, bool playSfx = false)
    {
        isOn = on;
        Refresh(playSfx);
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void OnClick()
    {
        isOn = !isOn;
        Refresh(playSfx: true);
        OnToggled?.Invoke(isOn);
    }

    private void Refresh(bool playSfx)
    {
        buttonImage.sprite     = isOn ? buttonSpriteOn     : buttonSpriteOff;
        backgroundImage.sprite = isOn ? backgroundSpriteOn : backgroundSpriteOff;

        button.GetComponent<RectTransform>().anchoredPosition = isOn ? buttonPosOn : buttonPosOff;
    }
}
