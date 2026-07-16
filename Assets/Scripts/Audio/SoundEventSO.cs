// ──────────────────────────────────────────────
// TheSprouty | Scripts/Audio/SoundEventSO.cs
// ScriptableObject representing a single sound effect.
// Assign a clip, tune volume and pitch, then call Play().
// ──────────────────────────────────────────────
using UnityEngine;

[CreateAssetMenu(menuName = "TheSprouty/Audio/Sound Event", fileName = "SFX_New")]
public class SoundEventSO : ScriptableObject
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------

    [Tooltip("One or more clips — a random one is picked each time Play() is called.")]
    public AudioClip[] clips;

    [Range(0f, 1f)]
    [Tooltip("Playback volume for this sound.")]
    public float volume = 1f;

    [Range(0.5f, 1.5f)]
    [Tooltip("Playback pitch for this sound.")]
    public float pitch = 1f;

    [Tooltip("If true, sound loops until StopSFXLoop() is called on AudioManager.")]
    public bool loop = false;

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------

    /// <summary>Play this sound through AudioManager (2D, no position).</summary>
    public void Play()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.PlaySFX(this);
    }

    /// <summary>Returns a random clip from the clips array. Null if array is empty.</summary>
    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }
}
