// ──────────────────────────────────────────────
// TheSprouty | UI/TargetFPSSelectorUI.cs
// Cycles through FPS options via prev/next buttons.
// Applies Application.targetFrameRate on change.
// ──────────────────────────────────────────────
using TMPro;
using UnityEngine;

public class TargetFPSSelectorUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private TMP_Text fpsText;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private const string PREF_FPS_INDEX = "settings_fps_index";

    private static readonly int[] FPS_OPTIONS = { 30, 60, 90, 120, 144 };
    private int _currentIndex;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Start()
    {
        _currentIndex = PlayerPrefs.GetInt(PREF_FPS_INDEX, 1); // default index 1 = 60
        Refresh();
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>Assign to LowerButton OnClick.</summary>
    public void SelectPrevious()
    {
        _currentIndex = (_currentIndex - 1 + FPS_OPTIONS.Length) % FPS_OPTIONS.Length;
        Refresh();
    }

    /// <summary>Assign to HigherButton OnClick.</summary>
    public void SelectNext()
    {
        _currentIndex = (_currentIndex + 1) % FPS_OPTIONS.Length;
        Refresh();
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void Refresh()
    {
        Application.targetFrameRate = FPS_OPTIONS[_currentIndex];
        fpsText.text = FPS_OPTIONS[_currentIndex].ToString();
        PlayerPrefs.SetInt(PREF_FPS_INDEX, _currentIndex);
        PlayerPrefs.Save();
    }
}
