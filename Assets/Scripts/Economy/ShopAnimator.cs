// ──────────────────────────────────────────────
// TheSprouty | Economy/ShopAnimator.cs
// Pop-scale animation for the ShopPanel.
// Scales from startScale → 1 on open, 1 → startScale on close.
// Attach on the ShopPanel GameObject.
// ──────────────────────────────────────────────
using System;
using System.Collections;
using UnityEngine;

public class ShopAnimator : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [Header("Animation")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private float         animDuration = 0.2f;
    [SerializeField] private float         startScale   = 0.7f;
    [SerializeField] private AnimationCurve scaleCurve;

    [Header("Sound")]
    [SerializeField] private SoundEventSO openSound;
    [SerializeField] private SoundEventSO closeSound;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private CanvasGroup _canvasGroup;
    private Coroutine   _animRoutine;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        SetVisible(false);
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>Plays open animation. onComplete fires when finished.</summary>
    public void Show(Action onComplete = null)
    {
        openSound?.Play();
        SetVisible(true);
        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(AnimateRoutine(startScale, 1f, onComplete));
    }

    /// <summary>Plays close animation then hides panel.</summary>
    public void Hide(Action onComplete = null)
    {
        closeSound?.Play();
        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(AnimateRoutine(1f, startScale, onComplete: () =>
        {
            SetVisible(false);
            onComplete?.Invoke();
        }));
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private IEnumerator AnimateRoutine(float fromScale, float toScale, Action onComplete = null)
    {
        float elapsed = 0f;
        panel.localScale = Vector3.one * fromScale;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = scaleCurve.Evaluate(Mathf.Clamp01(elapsed / animDuration));
            float s = Mathf.LerpUnclamped(fromScale, toScale, t);
            panel.localScale = Vector3.one * s;
            yield return null;
        }

        panel.localScale = Vector3.one * toScale;
        _animRoutine = null;
        onComplete?.Invoke();
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha          = visible ? 1f : 0f;
        _canvasGroup.interactable   = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
