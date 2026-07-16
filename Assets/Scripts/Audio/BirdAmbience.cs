// ──────────────────────────────────────────────
// TheSprouty | Scripts/Audio/BirdAmbience.cs
// Plays random bird chirp clips at random intervals,
// only during a configured in-game hour range.
// ──────────────────────────────────────────────
using System.Collections;
using UnityEngine;

public class BirdAmbience : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------

    [Header("Clips")]
    [Tooltip("Pool of bird chirp clips — one is picked at random each time.")]
    [SerializeField] private AudioClip[] chirpClips;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.6f;

    [Header("Interval")]
    [Tooltip("Minimum real seconds between chirps.")]
    [SerializeField] private float minInterval = 4f;

    [Tooltip("Maximum real seconds between chirps.")]
    [SerializeField] private float maxInterval = 12f;

    [Header("Fade")]
    [Tooltip("Seconds to fade in each chirp.")]
    [SerializeField] private float fadeInDuration  = 0.15f;

    [Tooltip("Seconds to fade out each chirp.")]
    [SerializeField] private float fadeOutDuration = 0.3f;

    [Header("Active Hours (in-game)")]
    [Tooltip("Birds start chirping at this in-game hour.")]
    [SerializeField] private float startHour = 7f;

    [Tooltip("Birds stop chirping at this in-game hour.")]
    [SerializeField] private float endHour = 10f;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------

    private AudioSource _audioSource;
    private Coroutine   _chirpRoutine;
    private float       _baseVolume;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _baseVolume  = volume;
    }

    private void Start()
    {
        if (DayCycleManager.Instance != null)
            DayCycleManager.Instance.OnHourChanged += HandleHourChanged;

        // Apply persisted multiplier if AudioManager is already up
        if (AudioManager.Instance != null)
            volume = _baseVolume * AudioManager.Instance.AmbienceMultiplier;

        AudioManager.OnAmbienceMultiplierChanged += HandleAmbienceMultiplierChanged;

        TryStartOrStop();
    }

    private void OnDestroy()
    {
        if (DayCycleManager.Instance != null)
            DayCycleManager.Instance.OnHourChanged -= HandleHourChanged;

        AudioManager.OnAmbienceMultiplierChanged -= HandleAmbienceMultiplierChanged;
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------

    private void HandleHourChanged(object sender, int hour)
    {
        TryStartOrStop();
    }

    private void HandleAmbienceMultiplierChanged(float multiplier)
    {
        volume = _baseVolume * multiplier;
    }

    private void TryStartOrStop()
    {
        bool shouldPlay = IsActiveHour();

        if (shouldPlay && _chirpRoutine == null)
            _chirpRoutine = StartCoroutine(ChirpRoutine());
        else if (!shouldPlay && _chirpRoutine != null)
        {
            StopCoroutine(_chirpRoutine);
            _chirpRoutine = null;
        }
    }

    private bool IsActiveHour()
    {
        if (DayCycleManager.Instance == null) return false;
        float hour = DayCycleManager.Instance.CurrentHour;
        return hour >= startHour && hour < endHour;
    }

    private IEnumerator ChirpRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            if (!IsActiveHour()) break;
            if (chirpClips == null || chirpClips.Length == 0) continue;

            AudioClip clip = chirpClips[Random.Range(0, chirpClips.Length)];
            if (clip != null)
                yield return StartCoroutine(PlayWithFadeRoutine(clip));
        }

        _chirpRoutine = null;
    }

    private IEnumerator PlayWithFadeRoutine(AudioClip clip)
    {
        _audioSource.clip   = clip;
        _audioSource.volume = 0f;
        _audioSource.Play();

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(0f, volume, elapsed / fadeInDuration);
            yield return null;
        }
        _audioSource.volume = volume;

        // Wait for clip to near its end before fading out
        float holdDuration = Mathf.Max(0f, clip.length - fadeInDuration - fadeOutDuration);
        yield return new WaitForSeconds(holdDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(volume, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        _audioSource.Stop();
        _audioSource.volume = 0f;
    }
}
