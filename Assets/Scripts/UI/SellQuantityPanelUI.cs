// ──────────────────────────────────────────────
// TheSprouty | UI/SellQuantityPanelUI.cs
// Popup for entering sell quantity.
// Clamps input to max available in inventory.
// Attach on SellQuantityPanel GameObject.
// ──────────────────────────────────────────────
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SellQuantityPanelUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Singleton
    // ----------------------------------------------------------
    public static SellQuantityPanelUI Instance { get; private set; }

    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private SoundEventSO   confirmSound;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private ItemSO      _currentItem;
    private int         _maxQuantity;
    private CanvasGroup _canvasGroup;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _canvasGroup = GetComponent<CanvasGroup>();
        SetVisible(false);
    }

    private void Start()
    {
        quantityInput.onValueChanged.AddListener(OnInputChanged);
    }

    private void OnDestroy()
    {
        quantityInput.onValueChanged.RemoveListener(OnInputChanged);
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>Shows the quantity input panel for the given item.</summary>
    public void Show(ItemSO item, int maxQuantity)
    {
        _currentItem       = item;
        _maxQuantity       = maxQuantity;
        quantityInput.text = "1";
        SetVisible(true);
        quantityInput.Select();
    }

    /// <summary>Hides the panel and clears state.</summary>
    public void Hide()
    {
        _currentItem = null;
        SetVisible(false);
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------
    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha          = visible ? 1f : 0f;
        _canvasGroup.interactable   = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    private void OnInputChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        if (int.TryParse(value, out int qty))
        {
            int clamped = Mathf.Clamp(qty, 1, _maxQuantity);
            if (clamped != qty)
                quantityInput.text = clamped.ToString();
        }
    }

    /// <summary>Confirms the sell quantity. Wire to ConfirmButton onClick in Inspector.</summary>
    public void OnConfirmClicked()
    {
        if (_currentItem == null) return;

        if (!int.TryParse(quantityInput.text, out int qty)) return;

        qty = Mathf.Clamp(qty, 1, _maxQuantity);
        confirmSound?.Play();
        SellPageUI.Instance.AddToSell(_currentItem, qty);

        Hide();
        ItemActionPanelUI.Instance.Hide();
    }
}
