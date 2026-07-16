// ──────────────────────────────────────────────
// TheSprouty | UI/MenuManager.cs
// Manages Main Menu UI — opens/closes panels.
// ──────────────────────────────────────────────
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private PopScaleAnimator savePanel;
    [SerializeField] private PopScaleAnimator aboutPanel;
    [SerializeField] private PopScaleAnimator creditsPanel;
    [SerializeField] private PopScaleAnimator exitGamePanel;
    [SerializeField] private PopScaleAnimator settingsPanel;
    [SerializeField] private PopScaleAnimator confirmRemovePanel;

    [Header("SFX")]
    [SerializeField] private SoundEventSO sfxPanelOpen;
    [SerializeField] private SoundEventSO sfxPanelClose;
    [SerializeField] private SoundEventSO sfxCancelExit;
    [SerializeField] private SoundEventSO sfxQuit;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Start()
    {
        savePanel.SetVisibleImmediate(false);
        aboutPanel.SetVisibleImmediate(false);
        creditsPanel.SetVisibleImmediate(false);
        exitGamePanel.SetVisibleImmediate(false);
        settingsPanel.SetVisibleImmediate(false);
        confirmRemovePanel.SetVisibleImmediate(false);
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------

    /// <summary>Opens the Save Panel. Assign to Play Button OnClick.</summary>
    public void OpenSavePanel()    => ShowPanel(savePanel);

    /// <summary>Closes the Save Panel. Assign to Close/Back Button OnClick.</summary>
    public void CloseSavePanel()   => HidePanel(savePanel);

    /// <summary>Opens the About Panel. Assign to About Button OnClick.</summary>
    public void OpenAboutPanel()   => ShowPanel(aboutPanel);

    /// <summary>Closes the About Panel. Assign to Close Button OnClick.</summary>
    public void CloseAboutPanel()  => HidePanel(aboutPanel);

    /// <summary>Opens the Credits Panel. Assign to Credits Button OnClick.</summary>
    public void OpenCreditsPanel() => ShowPanel(creditsPanel);

    /// <summary>Closes the Credits Panel. Assign to Close Button OnClick.</summary>
    public void CloseCreditsPanel() => HidePanel(creditsPanel);

    /// <summary>Opens the Settings Panel. Assign to Settings Button OnClick.</summary>
    public void OpenSettingsPanel()  => ShowPanel(settingsPanel);

    /// <summary>Closes the Settings Panel. Assign to Close Button OnClick.</summary>
    public void CloseSettingsPanel() => HidePanel(settingsPanel);

    /// <summary>Opens the Exit Game Panel. Assign to Exit Button OnClick.</summary>
    public void OpenExitGamePanel()  => ShowPanel(exitGamePanel);

    /// <summary>Closes the Exit Game Panel. Assign to Cancel Button OnClick.</summary>
    public void CloseExitGamePanel()
    {
        sfxCancelExit?.Play();
        exitGamePanel.Hide();
    }

    /// <summary>Quits the game. In Editor stops Play Mode; in build calls Application.Quit.</summary>
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
    private void ShowPanel(PopScaleAnimator animator)
    {
        sfxPanelOpen?.Play();
        animator.Show();
    }

    private void HidePanel(PopScaleAnimator animator)
    {
        sfxPanelClose?.Play();
        animator.Hide();
    }
}
