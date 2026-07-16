// ──────────────────────────────────────────────
// TheSprouty | UI/ItemActionPanelUI.cs
// Popup panel shown when player clicks an inventory slot.
// Repositions itself to the right of the clicked slot.
// Button onClick events are wired in the Inspector.
// ──────────────────────────────────────────────
using UnityEngine;

public class ItemActionPanelUI : MonoBehaviour
{
    // ----------------------------------------------------------
    // Singleton
    // ----------------------------------------------------------
    public static ItemActionPanelUI Instance { get; private set; }

    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [Header("Settings")]
    [Tooltip("Horizontal gap between slot right edge and panel left edge.")]
    [SerializeField] private float horizontalOffset = 8f;
    [SerializeField] private RectTransform sellQuantityPanelRect;

    [Header("Sound")]
    [SerializeField] private SoundEventSO dismissSound;
    [SerializeField] private SoundEventSO actionSound;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private RectTransform _panelRect;
    private Canvas _rootCanvas;
    private int _currentSlotIndex = -1;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _panelRect = GetComponent<RectTransform>();
        _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        bool insidePanel = RectTransformUtility.RectangleContainsScreenPoint(_panelRect, Input.mousePosition, _rootCanvas.worldCamera);

        bool insideSellQuantity = sellQuantityPanelRect != null
            && RectTransformUtility.RectangleContainsScreenPoint(sellQuantityPanelRect, Input.mousePosition, _rootCanvas.worldCamera);

        if (!insidePanel && !insideSellQuantity)
        {
            dismissSound?.Play();
            Hide();
        }
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    /// <summary>Shows the panel to the right of the given slot.</summary>
    public void Show(RectTransform slotRect, int slotIndex)
    {
        ItemSlot slot = InventoryManager.Instance.GetSlots()[slotIndex];
        if (slot.IsEmpty) { Hide(); return; }

        _currentSlotIndex = slotIndex;

        float slotHalfWidth  = slotRect.rect.width  * slotRect.lossyScale.x * 0.5f;
        float slotHalfHeight = slotRect.rect.height * slotRect.lossyScale.y * 0.5f;
        _panelRect.position = slotRect.position + new Vector3(slotHalfWidth + horizontalOffset, slotHalfHeight, 0f);

        gameObject.SetActive(true);
    }

    /// <summary>Hides the panel and clears tracked slot.</summary>
    public void Hide()
    {
        _currentSlotIndex = -1;
        gameObject.SetActive(false);
    }

    /// <summary>Opens quantity input panel for selling. Wire to SellButton.onClick.</summary>
    public void Sell()
    {
        if (_currentSlotIndex < 0) return;

        ItemSlot slot = InventoryManager.Instance.GetSlots()[_currentSlotIndex];
        if (slot.IsEmpty) { Hide(); return; }

        ItemSO item = slot.GetItemSO();
        if (item.sellValue <= 0) { Hide(); return; }

        actionSound?.Play();
        SellQuantityPanelUI.Instance.Show(item, slot.GetQuantity());
    }

    /// <summary>Moves all quantity of the current slot item to ToSell list. Wire to SellAllButton.onClick.</summary>
    public void SellAll()
    {
        if (_currentSlotIndex < 0) return;

        ItemSlot slot = InventoryManager.Instance.GetSlots()[_currentSlotIndex];
        if (slot.IsEmpty) { Hide(); return; }

        ItemSO item     = slot.GetItemSO();
        int    quantity = slot.GetQuantity();

        if (item.sellValue <= 0) { Hide(); return; }

        actionSound?.Play();
        SellPageUI.Instance.AddToSell(item, quantity);
        Hide();
    }

    /// <summary>Removes all quantity of the current slot item without gold. Wire to RemoveButton.onClick.</summary>
    public void Remove()
    {
        if (_currentSlotIndex < 0) return;

        ItemSlot slot = InventoryManager.Instance.GetSlots()[_currentSlotIndex];
        if (slot.IsEmpty) { Hide(); return; }

        actionSound?.Play();
        InventoryManager.Instance.RemoveItem(slot.GetItemSO(), slot.GetQuantity());
        Hide();
    }
}
