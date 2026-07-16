// ──────────────────────────────────────────────
// TheSprouty | UI/SceneTransitionManager.cs
// Snake-wipe / Circle-wipe scene transition.
// Singleton, DontDestroyOnLoad.
// Builds its own full-screen Canvas overlay at runtime.
// Usage: SceneTransitionManager.TransitionTo("MainScene");
//        SceneTransitionManager.TransitionTo("MainScene", type: TransitionType.Circle);
// ──────────────────────────────────────────────
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
public class SceneTransitionManager : MonoBehaviour
{
    // ----------------------------------------------------------
    // Enums
    // ----------------------------------------------------------

    public enum TransitionType { Snake = 0, Circle = 1, Random = 2 }

    // ----------------------------------------------------------
    // Events
    // ----------------------------------------------------------

    /// <summary>Fired when transition OUT begins (screen starts covering). Use for BGM fade out.</summary>
    public static event Action OnTransitionStarted;

    /// <summary>Fired when screen is fully covered (midpoint).</summary>
    public static event Action OnTransitionMidpoint;

    /// <summary>Fired when IN animation completes (scene fully visible).</summary>
    public static event Action OnTransitionComplete;

    // ----------------------------------------------------------
    // Singleton
    // ----------------------------------------------------------
    public static SceneTransitionManager Instance { get; private set; }

    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private Material transitionMaterial;

    [Header("Appearance")]
    [SerializeField] private Color transitionColor = new Color(1f, 0.973f, 0.933f, 1f);

    [Header("Timing (seconds)")]
    [SerializeField] private float outDuration = 1.2f;
    [SerializeField] private float inDuration  = 1.0f;

    [Header("Transition Type")]
    [SerializeField] private TransitionType transitionType = TransitionType.Snake;

    [Header("Snake Parameters")]
    [SerializeField] [Range(2, 20)] private int stripCount = 5;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private static readonly int PROP_LINEAR_PROGRESS = Shader.PropertyToID("_LinearProgress");
    private static readonly int PROP_COLOR           = Shader.PropertyToID("_Color");
    private static readonly int PROP_TRANSITION_TYPE = Shader.PropertyToID("_TransitionType");
    private static readonly int PROP_STRIPS          = Shader.PropertyToID("_StripCount");

    private Material    _runtimeMaterial;
    private CanvasGroup _canvasGroup;
    private bool        _isTransitioning;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateRuntimeMaterial();
        BuildOverlayCanvas();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------

    /// <summary>
    /// Starts scene transition: OUT covers screen → scene loads → IN reveals scene.
    /// Optionally override duration and transition type per-call.
    /// No-op if a transition is already running.
    /// </summary>
    public static void TransitionTo(string sceneName,
                                    float?          outDur = null,
                                    float?          inDur  = null,
                                    TransitionType? type   = null)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[SceneTransitionManager] No instance — loading directly.");
            SceneManager.LoadScene(sceneName);
            return;
        }

        if (Instance._isTransitioning) return;

        Instance.StartCoroutine(Instance.TransitionRoutine(sceneName, outDur, inDur, type));
    }

    // ----------------------------------------------------------
    // Private — coroutine
    // ----------------------------------------------------------
    private IEnumerator TransitionRoutine(string         sceneName,
                                          float?          outDur,
                                          float?          inDur,
                                          TransitionType? typeOverride)
    {
        _isTransitioning = true;

        float actualOut = outDur ?? outDuration;
        float actualIn  = inDur  ?? inDuration;

        // Resolve Random → pick Snake or Circle randomly
        TransitionType activeType = typeOverride ?? transitionType;
        if (activeType == TransitionType.Random)
            activeType = (TransitionType)UnityEngine.Random.Range(0, 2); // 0=Snake, 1=Circle

        // Show overlay immediately — instant visual feedback on click
        _runtimeMaterial.SetFloat(PROP_LINEAR_PROGRESS, 0f);
        ApplyMaterialParams(activeType);
        SetOverlayVisible(true);

        // Notify listeners (e.g. AudioManager) that OUT animation is starting
        OnTransitionStarted?.Invoke();
        yield return null;

        // OUT: cover screen (progress 0 → 1)
        yield return AnimateRoutine(0f, 1f, actualOut);
        OnTransitionMidpoint?.Invoke();

        // Load and activate scene while screen is fully covered
        // Any freeze/spike from LoadSceneAsync is hidden behind the overlay
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
        loadOp.allowSceneActivation = false;
        while (loadOp.progress < 0.9f) yield return null;
        loadOp.allowSceneActivation = true;
        yield return new WaitForEndOfFrame();
        yield return null;

        // IN: reveal screen (progress 1 → 0)
        // deltaTime is clamped inside AnimateRoutine so spike frames don't cause jumps
        yield return AnimateRoutine(1f, 0f, actualIn);

        _runtimeMaterial.SetFloat(PROP_LINEAR_PROGRESS, 0f);
        SetOverlayVisible(false);

        _isTransitioning = false;
        OnTransitionComplete?.Invoke();
    }

    private IEnumerator AnimateRoutine(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Clamp to 50 ms max so a scene-load frame spike doesn't jump the animation
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float t = Mathf.Clamp01(elapsed / duration);
            _runtimeMaterial.SetFloat(PROP_LINEAR_PROGRESS, Mathf.Lerp(from, to, t));
            yield return null;
        }

        _runtimeMaterial.SetFloat(PROP_LINEAR_PROGRESS, to);
    }

    // ----------------------------------------------------------
    // Private — setup
    // ----------------------------------------------------------
    private void CreateRuntimeMaterial()
    {
        if (transitionMaterial == null)
        {
            Debug.LogError("[SceneTransitionManager] transitionMaterial not assigned!");
            return;
        }
        _runtimeMaterial = Instantiate(transitionMaterial);
        _runtimeMaterial.SetFloat(PROP_LINEAR_PROGRESS, 0f);
    }

    private void BuildOverlayCanvas()
    {
        GameObject canvasGO = new GameObject("TransitionCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<GraphicRaycaster>();

        _canvasGroup                = canvasGO.AddComponent<CanvasGroup>();
        _canvasGroup.alpha          = 0f;
        _canvasGroup.interactable   = false;
        _canvasGroup.blocksRaycasts = false;

        GameObject imageGO = new GameObject("TransitionImage");
        imageGO.transform.SetParent(canvasGO.transform, false);

        RawImage rawImage = imageGO.AddComponent<RawImage>();
        rawImage.material = _runtimeMaterial;

        RectTransform rect = rawImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void ApplyMaterialParams(TransitionType activeType)
    {
        _runtimeMaterial.SetColor(PROP_COLOR,           transitionColor);
        _runtimeMaterial.SetFloat(PROP_TRANSITION_TYPE, (float)activeType);
        _runtimeMaterial.SetFloat(PROP_STRIPS,          stripCount);
    }

    private void SetOverlayVisible(bool visible)
    {
        _canvasGroup.alpha          = visible ? 1f : 0f;
        _canvasGroup.interactable   = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
