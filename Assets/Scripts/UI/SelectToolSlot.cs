using UnityEngine;

public class SelectToolSlot : MonoBehaviour
{
    [SerializeField] private ToolSO tool;
    [SerializeField] private GameObject toolSlotVisual;
    [SerializeField] private GameObject toolSlotSelectedVisual;
    [SerializeField] protected RadialToolWheelUI radialToolWheelUI;

    /// <summary>Exposed so RadialToolWheelUI can find a slot by its tool type.</summary>
    public ToolType ToolType => tool != null ? tool.toolType : ToolType.None;

    private void Start()
    {
        SetSelected(Player.Instance.EquippedToolSO == tool);
    }

    public virtual void SelectTool()
    {
        Player.Instance.ChangeEquippedTool(tool);
        radialToolWheelUI.OnSlotSelected(this);
        radialToolWheelUI.Close();
    }

    public void SetSelected(bool selected)
    {
        toolSlotVisual.SetActive(!selected);
        toolSlotSelectedVisual.SetActive(selected);
    }
}
