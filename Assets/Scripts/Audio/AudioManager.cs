// ──────────────────────────────────────────────
// TheSprouty | Scripts/Audio/AudioManager.cs
// Per-scene AudioManager — no DontDestroyOnLoad.
// BGM fades in on Start, fades out on scene transition.
// Volume uses multiplier approach: actual = baseVolume × multiplier.
// Multipliers persisted via PlayerPrefs.
// ──────────────────────────────────────────────
using System;
using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------

    [Header("Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource sfxLoopSource;
    [SerializeField] private AudioSource ambienceSource; // optional — null in Menu Scene

    [Header("Fade Settings")]
    [Tooltip("Duration (seconds) for BGM to fade in when scene loads.")]
    [SerializeField] private float bgmFadeInDuration  = 1.5f;

    [Tooltip("Duration (seconds) for BGM to fade out on scene transition.")]
    [SerializeField] private float bgmFadeOutDuration = 1.5f;

    [Tooltip("Duration (seconds) for BGM to fade out when player sleeps.")]
    [SerializeField] private float sleepFadeOutDuration = 1.5f;

    [Tooltip("Duration (seconds) for BGM to fade in when player wakes up.")]
    [SerializeField] private float sleepFadeInDuration  = 1.5f;

    // ----------------------------------------------------------
    // PlayerPrefs keys
    // ----------------------------------------------------------

    private const string PREF_BGM_MULT      = "settings_bgm_mult";
    private const string PREF_SFX_MULT      = "settings_sfx_mult";
    private const string PREF_AMBIENCE_MULT = "settings_ambience_mult";

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------

    private float _bgmBaseVolume;
    private float _sfxBaseVolume;
    private float _ambienceBaseVolume;

    private float _bgmMultiplier;
    private float _sfxMultiplier;
    private float _ambienceMultiplier;

    private float     _targetVolume;
    private Coroutine _fadeRoutine;

    // ----------------------------------------------------------
    // Events
    // ----------------------------------------------------------

    /// <summary>Fired when ambience multiplier changes. BirdAmbience subscribes to this.</summary>
    public static event Action<float> OnAmbienceMultiplierChanged;

    // ----------------------------------------------------------
    // Properties
    // ----------------------------------------------------------

    public static AudioManager Instance { get; private set; }

    public float BGMMultiplier      => _bgmMultiplier;
    public float SFXMultiplier      => _sfxMultiplier;
    public float AmbienceMultiplier => _ambienceMultiplier;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------

    private void Awake()
    {
        Instance = this;

        // Store inspector values as base volumes
        _bgmBaseVolume      = bgmSource      != null ? bgmSource.volume      : 1f;
        _sfxBaseVolume      = sfxSource      != null ? sfxSource.volume      : 1f;
        _ambienceBaseVolume = ambienceSource != null ? ambienceSource.volume : 1f;

        // Load persisted multipliers
        _bgmMultiplier      = PlayerPrefs.GetFloat(PREF_BGM_MULT,      0.5f);
        _sfxMultiplier      = PlayerPrefs.GetFloat(PREF_SFX_MULT,      0.5f);
        _ambienceMultiplier = PlayerPrefs.GetFloat(PREF_AMBIENCE_MULT, 0.5f);

        // Apply SFX and ambience immediately (no fade needed)
        ApplySFXMultiplier();
        ApplyAmbienceMultiplier();
    }

    private void Start()
    {
        if (bgmSource != null)
        {
            _targetVolume    = _bgmBaseVolume * _bgmMultiplier;
            bgmSource.volume = 0f;
            bgmSource.Play();
            StartFade(_targetVolume, bgmFadeInDuration);
        }

        SceneTransitionManager.OnTransitionStarted += HandleTransitionStarted;

        if (DayCycleManager.Instance != null)
        {
            DayCycleManager.Instance.OnSleepStarted += HandleSleepStarted;
            DayCycleManager.Instance.OnSleepEnded   += HandleSleepEnded;
        }
    }

    private void OnDestroy()
    {
        SceneTransitionManager.OnTransitionStarted -= HandleTransitionStarted;

        if (DayCycleManager.Instance != null)
        {
            DayCycleManager.Instance.OnSleepStarted -= HandleSleepStarted;
            DayCycleManager.Instance.OnSleepEnded   -= HandleSleepEnded;
        }

        if (Instance == this) Instance = null;
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------

    /// <summary>
    /// Set BGM multiplier (0–1). Actual volume = inspector volume × multiplier.
    /// Persisted via PlayerPrefs.
    /// </summary>
    public void SetBGMVolume(float multiplier)
    {
        _bgmMultiplier = Mathf.Clamp01(multiplier);
        _targetVolume  = _bgmBaseVolume * _bgmMultiplier;

        if (_fadeRoutine == null && bgmSource != null)
            bgmSource.volume = _targetVolume;

        PlayerPrefs.SetFloat(PREF_BGM_MULT, _bgmMultiplier);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Set SFX multiplier (0–1). Applies to both sfxSource and sfxLoopSource.
    /// Persisted via PlayerPrefs.
    /// </summary>
    public void SetSFXVolume(float multiplier)
    {
        _sfxMultiplier = Mathf.Clamp01(multiplier);
        ApplySFXMultiplier();

        PlayerPrefs.SetFloat(PREF_SFX_MULT, _sfxMultiplier);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Set Ambience multiplier (0–1). Applies to ambienceSource and fires
    /// OnAmbienceMultiplierChanged for BirdAmbience.
    /// Persisted via PlayerPrefs.
    /// </summary>
    public void SetAmbienceVolume(float multiplier)
    {
        _ambienceMultiplier = Mathf.Clamp01(multiplier);
        ApplyAmbienceMultiplier();
        OnAmbienceMultiplierChanged?.Invoke(_ambienceMultiplier);

        PlayerPrefs.SetFloat(PREF_AMBIENCE_MULT, _ambienceMultiplier);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Play a sound effect.
    /// Non-loop sounds use PlayOneShot (supports overlap).
    /// Loop sounds play on a dedicated source — call StopSFXLoop() to stop.
    /// </summary>
    public void PlaySFX(SoundEventSO soundEvent)
    {
        if (soundEvent == null) return;

        AudioClip clip = soundEvent.GetRandomClip();
        if (clip == null) return;

        if (soundEvent.loop)
        {
            if (sfxLoopSource == null) return;
            sfxLoopSource.clip   = clip;
            sfxLoopSource.volume = soundEvent.volume * _sfxMultiplier;
            sfxLoopSource.pitch  = soundEvent.pitch;
            sfxLoopSource.loop   = true;
            sfxLoopSource.Play();
        }
        else
        {
            if (sfxSource == null) return;
            sfxSource.pitch = soundEvent.pitch;
            sfxSource.PlayOneShot(clip, soundEvent.volume);
        }
    }

    /// <summary>Stops the currently looping SFX.</summary>
    public void StopSFXLoop()
    {
        if (sfxLoopSource == null) return;
        sfxLoopSource.Stop();
        sfxLoopSource.clip = null;
    }

    // ----------------------------------------------------------
    // Private — apply helpers
    // ----------------------------------------------------------

    private void ApplySFXMultiplier()
    {
        if (sfxSource      != null) sfxSource.volume      = _sfxBaseVolume * _sfxMultiplier;
        if (sfxLoopSource  != null) sfxLoopSource.volume  = _sfxBaseVolume * _sfxMultiplier;
    }

    private void ApplyAmbienceMultiplier()
    {
        if (ambienceSource != null)
            ambienceSource.volume = _ambienceBaseVolume * _ambienceMultiplier;
    }

    // ----------------------------------------------------------
    // Private — event handlers
    // ----------------------------------------------------------

    private void HandleTransitionStarted()
    {
        StartFade(0f, bgmFadeOutDuration);
    }

    private void HandleSleepStarted(object sender, EventArgs e)
    {
        StartFade(0f, sleepFadeOutDuration);
    }

    private void HandleSleepEnded(object sender, EventArgs e)
    {
        StartFade(_targetVolume, sleepFadeInDuration);
    }

    // ----------------------------------------------------------
    // Private — fade
    // ----------------------------------------------------------

    private void StartFade(float toVolume, float duration)
    {
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(FadeRoutine(toVolume, duration));
    }

    private IEnumerator FadeRoutine(float toVolume, float duration)
    {
        if (bgmSource == null) yield break;

        float fromVolume = bgmSource.volume;
        float elapsed    = 0f;

        while (elapsed < duration)
        {
            elapsed          += Time.deltaTime;
            bgmSource.volume  = Mathf.Lerp(fromVolume, toVolume, elapsed / duration);
            yield return null;
        }

        bgmSource.volume = toVolume;
        _fadeRoutine     = null;
    }
}
