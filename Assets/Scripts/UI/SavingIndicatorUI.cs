// ──────────────────────────────────────────────
// TheSprouty | Scripts/UI/SavingIndicatorUI.cs
// Spinning icon + "Saving..." text shown while saving.
// ──────────────────────────────────────────────
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SavingIndicatorUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------

    [Header("References")]
    [SerializeField] private Image       spinIcon;
    [SerializeField] private TMP_Text    label;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [Tooltip("Degrees per second.")]
    [SerializeField] private float spinSpeed    = 360f;
    [Tooltip("Seconds to fade in / out.")]
    [SerializeField] private float fadeDuration = 0.3f;
    [Tooltip("Seconds the indicator stays visible after save completes.")]
    [SerializeField] private float holdDuration = 0.8f;
    [SerializeField] private string savingText  = "Saving...";
    [SerializeField] private string savedText   = "Saved!";

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private bool      _isSpinning;
    private Coroutine _fadeRoutine;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        SaveManager.OnSaveStarted   += HandleSaveStarted;
        SaveManager.OnSaveCompleted += HandleSaveCompleted;
    }

    private void OnDisable()
    {
        SaveManager.OnSaveStarted   -= HandleSaveStarted;
        SaveManager.OnSaveCompleted -= HandleSaveCompleted;
    }

    private void Update()
    {
        if (!_isSpinning || spinIcon == null) return;
        spinIcon.rectTransform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
    }

    // ----------------------------------------------------------
    // Event handlers
    // ----------------------------------------------------------
    private void HandleSaveStarted()
    {
        _isSpinning = true;
        SetLabel(savingText);

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FullCycleRoutine());
    }

    private void HandleSaveCompleted()
    {
        // Save is synchronous — OnSaveCompleted fires in the same frame as OnSaveStarted.
        // Animation is driven entirely by FullCycleRoutine, not by this event.
    }

    // ----------------------------------------------------------
    // Private helpers
    // ----------------------------------------------------------
    private void SetLabel(string text)
    {
        if (label != null) label.text = text;
    }

    private IEnumerator FullCycleRoutine()
    {
        // Fade in
        yield return FadeToAlpha(0f, 1f, fadeDuration);

        // Show "Saving..." while spinning
        yield return new WaitForSeconds(holdDuration * 0.4f);

        // Switch to "Saved!"
        SetLabel(savedText);

        // Hold a moment
        yield return new WaitForSeconds(holdDuration * 0.6f);

        // Fade out
        yield return FadeToAlpha(1f, 0f, fadeDuration);

        _isSpinning = false;
    }

    private System.Collections.IEnumerator FadeToAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed           += Time.deltaTime;
            canvasGroup.alpha  = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
