// ──────────────────────────────────────────────
// TheSprouty | UI/SettingsPanelUI.cs
// Connects Settings panel controls to AudioManager,
// Screen fullscreen, and Application.targetFrameRate.
// Loads persisted values on Start, saves on change.
// ──────────────────────────────────────────────
using UnityEngine;

public class SettingsPanelUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------

    [Header("Sound Sliders")]
    [SerializeField] private SegmentedSliderUI musicSlider;
    [SerializeField] private SegmentedSliderUI sfxSlider;
    [SerializeField] private SegmentedSliderUI ambienceSlider;

    [Header("Fullscreen Toggle")]
    [SerializeField] private ToggleSwitchUI fullscreenToggle;

    // ----------------------------------------------------------
    // PlayerPrefs keys
    // ----------------------------------------------------------

    private const string PREF_FULLSCREEN = "settings_fullscreen";

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------

    private void Start()
    {
        InitSliders();
        InitFullscreen();
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // ----------------------------------------------------------
    // Private — init
    // ----------------------------------------------------------

    private void InitSliders()
    {
        if (AudioManager.Instance == null) return;

        // Set slider values from persisted multipliers (no event fired on init)
        musicSlider.Value    = AudioManager.Instance.BGMMultiplier;
        sfxSlider.Value      = AudioManager.Instance.SFXMultiplier;
        ambienceSlider.Value = AudioManager.Instance.AmbienceMultiplier;
    }

    private void InitFullscreen()
    {
        bool isFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, 1) == 1;
        fullscreenToggle.SetState(isFullscreen, playSfx: false);
        ApplyFullscreen(isFullscreen);
    }

    // ----------------------------------------------------------
    // Private — subscribe / unsubscribe
    // ----------------------------------------------------------

    private void SubscribeEvents()
    {
        musicSlider.OnValueChanged    += HandleMusicChanged;
        sfxSlider.OnValueChanged      += HandleSFXChanged;
        ambienceSlider.OnValueChanged += HandleAmbienceChanged;
        fullscreenToggle.OnToggled    += HandleFullscreenToggled;
    }

    private void UnsubscribeEvents()
    {
        musicSlider.OnValueChanged    -= HandleMusicChanged;
        sfxSlider.OnValueChanged      -= HandleSFXChanged;
        ambienceSlider.OnValueChanged -= HandleAmbienceChanged;
        fullscreenToggle.OnToggled    -= HandleFullscreenToggled;
    }

    // ----------------------------------------------------------
    // Private — handlers
    // ----------------------------------------------------------

    private void HandleMusicChanged(float value)
    {
        AudioManager.Instance?.SetBGMVolume(value);
    }

    private void HandleSFXChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
    }

    private void HandleAmbienceChanged(float value)
    {
        AudioManager.Instance?.SetAmbienceVolume(value);
    }

    private void HandleFullscreenToggled(bool isOn)
    {
        ApplyFullscreen(isOn);
        PlayerPrefs.SetInt(PREF_FULLSCREEN, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyFullscreen(bool isOn)
    {
        Screen.fullScreenMode = isOn
            ? FullScreenMode.ExclusiveFullScreen
            : FullScreenMode.Windowed;
    }
}
