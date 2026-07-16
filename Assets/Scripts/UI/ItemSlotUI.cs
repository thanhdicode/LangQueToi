using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private UnityEngine.UI.Image itemIcon;
    [SerializeField] private TMPro.TextMeshProUGUI quantityText;
    [SerializeField] private UnityEngine.UI.Image background;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0, 0, 0, 0.3f);
    [SerializeField] private Color hoverColor = new Color(0, 0, 0, 0.7f);

    [Header("Drag")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Sound")]
    [SerializeField] private SoundEventSO clickSound;

    private int _slotIndex;
    private InventoryUI _inventoryUI;
    private GameObject _dragIcon;
    private RectTransform _dragIconRT;
    private Canvas _rootCanvas;

    private void Awake()
    {
        _rootCanvas = transform.root.GetComponent<Canvas>();
    }

    private void Update()
    {
        if (_dragIcon == null) return;

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            _rootCanvas.transform as RectTransform,
            Input.mousePosition,
            _rootCanvas.worldCamera,
            out Vector3 worldPoint
        );
        _dragIconRT.position = worldPoint;
    }

    public void Setup(int index, InventoryUI inventoryUI)
    {
        _slotIndex   = index;
        _inventoryUI = inventoryUI;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        background.color = hoverColor;

        // Skip name display while user is dragging another slot
        if (eventData.dragging) return;
        UpdateCurrentItemText();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        background.color = normalColor;

        if (eventData.dragging) return;
        ClearCurrentItemText();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Không drag nếu slot trống
        if (InventoryManager.Instance.GetSlots()[_slotIndex].IsEmpty)
        {
            eventData.pointerDrag = null;
            return;
        }

        // Dim icon gốc trong slot để biết đang bị drag
        itemIcon.color = new Color(1, 1, 1, 0.4f);
        quantityText.gameObject.SetActive(false);

        // Tạo drag visual theo cursor
        _dragIcon = new GameObject("DragIcon");
        _dragIcon.transform.SetParent(transform.root);
        _dragIcon.transform.SetAsLastSibling(); // nằm trên cùng

        var image = _dragIcon.AddComponent<UnityEngine.UI.Image>();
        image.sprite = itemIcon.sprite;
        image.raycastTarget = false;

        _dragIconRT = _dragIcon.GetComponent<RectTransform>();
        _dragIconRT.sizeDelta = itemIcon.rectTransform.sizeDelta;

        canvasGroup.blocksRaycasts = false;
        CursorManager.Instance.SetDrag();

        // Hide name label during the drag — gets restored on next PointerEnter
        ClearCurrentItemText();
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragIcon == null) return;

        Destroy(_dragIcon);
        _dragIcon = null;
        itemIcon.color = Color.white;
        canvasGroup.blocksRaycasts = true;
        CursorManager.Instance.SetDefault();

        ItemSlotUI dropSlot = eventData.pointerEnter?.GetComponent<ItemSlotUI>();
        if (dropSlot != null && dropSlot != this)
            InventoryManager.Instance.SwapSlots(_slotIndex, dropSlot._slotIndex);
        else
            RefreshSelf();
    }

    public void Bind(ItemSlot itemSlot)
    {
        if (itemSlot.IsEmpty)
        {
            itemIcon.gameObject.SetActive(false);
            quantityText.gameObject.SetActive(false);
            return;
        }

        itemIcon.sprite = itemSlot.GetItemSO().icon;
        itemIcon.gameObject.SetActive(true);

        quantityText.text = itemSlot.GetQuantity().ToString();
        quantityText.gameObject.SetActive(itemSlot.GetQuantity() > 1);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Ignore click nếu vừa drag xong
        if (eventData.dragging) return;

        ItemSlot slot = InventoryManager.Instance.GetSlots()[_slotIndex];
        if (slot.IsEmpty)
        {
            ItemActionPanelUI.Instance.Hide();
            return;
        }

        clickSound?.Play();
        ItemActionPanelUI.Instance.Show(GetComponent<RectTransform>(), _slotIndex);
    }

    private void RefreshSelf()
    {
        Bind(InventoryManager.Instance.GetSlots()[_slotIndex]);
    }

    private void UpdateCurrentItemText()
    {
        if (_inventoryUI == null) return;

        ItemSlot slot = InventoryManager.Instance.GetSlots()[_slotIndex];
        if (slot.IsEmpty)
        {
            _inventoryUI.SetCurrentItemText(string.Empty);
            return;
        }

        _inventoryUI.SetCurrentItemText(slot.GetItemSO().itemName);
    }

    private void ClearCurrentItemText()
    {
        if (_inventoryUI == null) return;
        _inventoryUI.SetCurrentItemText(string.Empty);
    }
}
