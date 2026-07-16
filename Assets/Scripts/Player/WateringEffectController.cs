// ──────────────────────────────────────────────
// TheSprouty | Scripts/Player/WateringEffectController.cs
// Plays a directional watering-can spray effect by cycling through
// sprite frames. Triggered by PlayerAnimator at the raise-can frame.
// ──────────────────────────────────────────────
using System.Collections;
using UnityEngine;

public class WateringEffectController : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------

    [Header("Sprites — one array per direction")]
    [SerializeField] private Sprite[] frontSprites;
    [SerializeField] private Sprite[] leftSprites;
    [SerializeField] private Sprite[] rightSprites;

    [Header("Playback")]
    [Tooltip("Frames per second for the sprite animation.")]
    [SerializeField] private float fps = 12f;

    [Header("Sound")]
    [SerializeField] private SoundEventSO waterSound;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------

    private SpriteRenderer _spriteRenderer;
    private Coroutine      _playRoutine;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        gameObject.SetActive(false);
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------

    /// <summary>
    /// Plays the watering effect for the given direction.
    /// Calling Play() while already playing restarts the effect.
    /// </summary>
    public void Play(WateringDirection direction)
    {
        waterSound?.Play();
        if (_playRoutine != null) StopCoroutine(_playRoutine);
        gameObject.SetActive(true);
        _playRoutine = StartCoroutine(PlayRoutine(direction));
    }

    /// <summary>Stops and hides the effect immediately.</summary>
    public void Stop()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }
        gameObject.SetActive(false);
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------

    private IEnumerator PlayRoutine(WateringDirection direction)
    {
        Sprite[] frames = direction switch
        {
            WateringDirection.Left  => leftSprites,
            WateringDirection.Right => rightSprites,
            _                       => frontSprites   // Front + Back fallback
        };

        if (frames == null || frames.Length == 0)
        {
            gameObject.SetActive(false);
            yield break;
        }

        float interval = 1f / Mathf.Max(fps, 1f);

        foreach (Sprite frame in frames)
        {
            _spriteRenderer.sprite = frame;
            yield return new WaitForSeconds(interval);
        }

        _playRoutine = null;
        gameObject.SetActive(false);
    }
}
