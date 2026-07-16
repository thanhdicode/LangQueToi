// ──────────────────────────────────────────────
// TheSprouty | UI/PauseManager.cs
// Handles PausePanel toggle via ESC.
// Opens/closes SettingsPanel from within PausePanel.
// Back to Menu resets timeScale before transitioning.
// ──────────────────────────────────────────────
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private GameInput        gameInput;
    [SerializeField] private PopScaleAnimator pausePanel;
    [SerializeField] private PopScaleAnimator settingsPanel;
    [SerializeField] private PopScaleAnimator exitGamePanel;

    [Header("SFX")]
    [SerializeField] private SoundEventSO sfxOpen;
    [SerializeField] private SoundEventSO sfxClose;
    [SerializeField] private SoundEventSO sfxCancelExit;
    [SerializeField] private SoundEventSO sfxQuit;

    // ----------------------------------------------------------
    // Properties
    // ----------------------------------------------------------
    public static bool IsPaused { get; private set; }

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private bool _settingsOpen;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Start()
    {
        pausePanel.SetVisibleImmediate(false);
        settingsPanel.SetVisibleImmediate(false);
        exitGamePanel.SetVisibleImmediate(false);

        gameInput.OnTogglePausePanelAction += HandleTogglePause;
    }

    private void OnDestroy()
    {
        gameInput.OnTogglePausePanelAction -= HandleTogglePause;
    }

    // ----------------------------------------------------------
    // Public API — assign to buttons OnClick
    // ----------------------------------------------------------

    /// <summary>Resume game. Assign to Resume Button OnClick.</summary>
    public void Resume()
    {
        SetPaused(false);
    }

    /// <summary>Opens Settings panel. Assign to Settings Button OnClick.</summary>
    public void OpenSettings()
    {
        _settingsOpen = true;
        sfxOpen?.Play();
        settingsPanel.Show();
    }

    /// <summary>Closes Settings panel. Assign to Settings close/back Button OnClick.</summary>
    public void CloseSettings()
    {
        _settingsOpen = false;
        sfxClose?.Play();
        settingsPanel.Hide();
    }

    /// <summary>Returns to Menu Scene. Assign to Back to Menu Button OnClick.</summary>
    public void BackToMenu()
    {
        // Reset state without playing hide animation — scene transition covers everything.
        IsPaused       = false;
        _settingsOpen  = false;
        Time.timeScale = 1f;
        SceneTransitionManager.TransitionTo("MenuScene");
    }

    /// <summary>Opens Exit Game Panel. Assign to Exit Game Button OnClick.</summary>
    public void OpenExitGamePanel()
    {
        sfxOpen?.Play();
        exitGamePanel.Show();
    }

    /// <summary>Closes Exit Game Panel. Assign to Cancel Button OnClick.</summary>
    public void CloseExitGamePanel()
    {
        sfxCancelExit?.Play();
        exitGamePanel.Hide();
    }

    /// <summary>Quits the game. Assign to Confirm Button OnClick.</summary>
    public void QuitGame()
    {
        sfxQuit?.Play();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void HandleTogglePause(object sender, System.EventArgs e)
    {
        // ESC when Settings open → close Settings first
        if (_settingsOpen)
        {
            CloseSettings();
            return;
        }

        SetPaused(!IsPaused);
    }

    private void SetPaused(bool paused)
    {
        IsPaused       = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (paused)
        {
            sfxOpen?.Play();
            pausePanel.Show();
        }
        else
        {
            sfxClose?.Play();
            pausePanel.Hide();
        }
    }
}
