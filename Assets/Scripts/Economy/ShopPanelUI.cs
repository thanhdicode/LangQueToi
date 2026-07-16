// ──────────────────────────────────────────────
// TheSprouty | Economy/ShopPanelUI.cs
// Manages opening, closing and tab switching for the ShopPanel.
// Attach directly on the ShopPanel GameObject.
// ──────────────────────────────────────────────
using System.Collections;
using UnityEngine;

public class ShopPanelUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Singleton
    // ----------------------------------------------------------
    public static ShopPanelUI Instance { get; private set; }

    // ----------------------------------------------------------
    // Properties
    // ----------------------------------------------------------
    public static bool IsOpen { get; private set; }

    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [Header("Panels to hide while Shop is open")]
    [SerializeField] private GameObject[] panelsToHide;

    [Header("Pages")]
    [SerializeField] private CanvasGroup buyPageGroup;
    [SerializeField] private CanvasGroup sellPageGroup;

    [Header("Tabs")]
    [SerializeField] private RectTransform buyTabRect;
    [SerializeField] private RectTransform sellTabRect;
    [SerializeField] private float activeTabWidth   = 120f;
    [SerializeField] private float inactiveTabWidth = 90f;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Sound")]
    [SerializeField] private SoundEventSO switchPageSound;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private Coroutine    _fadeRoutine;
    private ShopAnimator _shopAnimator;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _shopAnimator = GetComponent<ShopAnimator>();
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>Opens the shop panel after dialogue close animation finishes.</summary>
    public void Open()
    {
        DialoguePanelUI.Instance.Close(onComplete: () =>
        {
            IsOpen = true;
            Player.Instance.EnterShop();

            foreach (var panel in panelsToHide)
                panel.SetActive(false);

            BuyPageUI.Instance.Initialize();
            SellPageUI.Instance.Initialize();
            SetPageImmediate(buyPageGroup, sellPageGroup);
            SetTabWidths(buyTabRect, sellTabRect);
            _shopAnimator.Show();
        });
    }

    /// <summary>Closes the shop panel and restores hidden UI panels.
    /// Wire to ExitButton onClick.</summary>
    public void Close()
    {
        IsOpen = false;
        Player.Instance.ExitShop();

        foreach (var panel in panelsToHide)
            panel.SetActive(true);

        _shopAnimator.Hide();
    }

    /// <summary>Switches to Buy page. Wire to BuyTabButton onClick.</summary>
    public void ShowBuyTab()
    {
        SetTabWidths(buyTabRect, sellTabRect);
        SwitchPage(buyPageGroup, sellPageGroup);
    }

    /// <summary>Switches to Sell page. Wire to SellTabButton onClick.</summary>
    public void ShowSellTab()
    {
        SetTabWidths(sellTabRect, buyTabRect);
        SwitchPage(sellPageGroup, buyPageGroup);
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void SetPageImmediate(CanvasGroup show, CanvasGroup hide)
    {
        show.alpha = 1f; show.interactable = true;  show.blocksRaycasts = true;
        hide.alpha = 0f; hide.interactable = false; hide.blocksRaycasts = false;
        show.gameObject.SetActive(true);
        hide.gameObject.SetActive(false);
    }

    private void SetTabWidths(RectTransform activeTab, RectTransform inactiveTab)
    {
        activeTab.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, activeTabWidth);
        inactiveTab.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, inactiveTabWidth);
    }

    private void SwitchPage(CanvasGroup fadeIn, CanvasGroup fadeOut)
    {
        switchPageSound?.Play();
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeRoutine(fadeIn, fadeOut));
    }

    private IEnumerator FadeRoutine(CanvasGroup fadeIn, CanvasGroup fadeOut)
    {
        fadeIn.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            fadeOut.alpha = 1f - t;
            fadeIn.alpha  = t;
            yield return null;
        }

        fadeOut.alpha = 0f;
        fadeIn.alpha  = 1f;
        fadeOut.gameObject.SetActive(false);

        fadeIn.interactable    = true;
        fadeIn.blocksRaycasts  = true;
        fadeOut.interactable   = false;
        fadeOut.blocksRaycasts = false;

        _fadeRoutine = null;
    }
}
