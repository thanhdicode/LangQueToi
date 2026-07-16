// ──────────────────────────────────────────────
// TheSprouty | UI/SegmentedSliderUI.cs
// Visual-only segmented slider — segments swap between
// filled/empty sprites based on Slider value.
// ──────────────────────────────────────────────
using System;
using UnityEngine;
using UnityEngine.UI;

public class SegmentedSliderUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private Slider    slider;
    [SerializeField] private Image[]   segments;
    [SerializeField] private Sprite    filledSprite;
    [SerializeField] private Sprite    emptySprite;

    // ----------------------------------------------------------
    // Events
    // ----------------------------------------------------------
    public event Action<float> OnValueChanged;

    // ----------------------------------------------------------
    // Properties
    // ----------------------------------------------------------

    /// <summary>Get or set slider value (0–1) from code.</summary>
    public float Value
    {
        get => slider.value;
        set => slider.value = value; // triggers onValueChanged → RefreshSegments
    }

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        slider.onValueChanged.AddListener(HandleValueChanged);
    }

    private void Start()
    {
        RefreshSegments(slider.value); // init visual without firing event
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(HandleValueChanged);
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void HandleValueChanged(float value)
    {
        RefreshSegments(value);
        OnValueChanged?.Invoke(value);
    }

    private void RefreshSegments(float value)
    {
        int filledCount = Mathf.RoundToInt(value * segments.Length);

        for (int i = 0; i < segments.Length; i++)
        {
            segments[i].sprite = i < filledCount ? filledSprite : emptySprite;
        }
    }
}
