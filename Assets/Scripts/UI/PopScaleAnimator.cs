// ──────────────────────────────────────────────
// TheSprouty | UI/PopScaleAnimator.cs
// Reusable pop-scale animation for any panel.
// Scales from startScale → 1 on Show, 1 → startScale on Hide.
// Manages its own CanvasGroup visibility.
// ──────────────────────────────────────────────
using System;
using System.Collections;
using UnityEngine;

public class PopScaleAnimator : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [Header("Animation")]
    [SerializeField] private RectTransform  panel;
    [SerializeField] private float          animDuration = 0.2f;
    [SerializeField] private float          startScale   = 0.7f;
    [SerializeField] private AnimationCurve scaleCurve   = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private CanvasGroup _canvasGroup;
    private Coroutine   _animRoutine;

    // ----------------------------------------------------------
    // Properties
    // ----------------------------------------------------------
    public bool IsVisible { get; private set; }

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

    /// <summary>Plays pop-in animation and shows the panel.</summary>
    public void Show(Action onComplete = null)
    {
        SetVisible(true);
        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(AnimateRoutine(startScale, 1f, onComplete));
    }

    /// <summary>Plays pop-out animation then hides the panel.</summary>
    public void Hide(Action onComplete = null)
    {
        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(AnimateRoutine(1f, startScale, onComplete: () =>
        {
            SetVisible(false);
            onComplete?.Invoke();
        }));
    }

    /// <summary>Shows or hides the panel instantly without animation.</summary>
    public void SetVisibleImmediate(bool visible)
    {
        if (_animRoutine != null)
        {
            StopCoroutine(_animRoutine);
            _animRoutine = null;
        }

        panel.localScale = Vector3.one * (visible ? 1f : startScale);
        SetVisible(visible);
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
            elapsed += Time.unscaledDeltaTime; // unscaled so animation works while paused
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
        IsVisible                    = visible;
        _canvasGroup.alpha          = visible ? 1f : 0f;
        _canvasGroup.interactable   = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
