// ──────────────────────────────────────────────
// TheSprouty | Scripts/ResourceNode.cs
// Abstract base for all harvestable resource nodes.
// Implements IDamageable, IInteractable, IDroppable.
// ──────────────────────────────────────────────
using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class ResourceNode : MonoBehaviour, IDamageable, IInteractable, IDroppable
{
    // ----------------------------------------------------------
    // Events
    // ----------------------------------------------------------

    /// <summary>
    /// Fired when any ResourceNode is fully destroyed (health = 0).
    /// Carries the node's stable scene ID so SaveManager can track it.
    /// </summary>
    public static event Action<string> OnNodeDestroyed;

    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------

    [Header("Resource Config")]
    [SerializeField] private ResourceNodeSO resourceNodeSO;
    [field: SerializeField] protected HarvestRecipeSO harvestRecipeSO { get; set; }

    [Header("Save")]
    [Tooltip("Stable ID for save/load — unique in the scene. Auto-generate via context menu.")]
    [SerializeField] private string nodeID;

    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------

    private int _currentHealth;

    // ----------------------------------------------------------
    // Properties
    // ----------------------------------------------------------

    public bool IsAlive    => _currentHealth > 0;
    public string NodeID   => nodeID;
    public int CurrentHealth => _currentHealth;
    public int MaxHealth   => resourceNodeSO != null ? resourceNodeSO.maxHealth : 0;

    /// <summary>
    /// Override in subclasses that have a fruit/secondary-drop state (e.g. FruitTree).
    /// Base always returns true — meaning "no special fruit state to track".
    /// </summary>
    public virtual bool HasFruit => true;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------

    protected virtual void Start()
    {
        if (resourceNodeSO != null)
            _currentHealth = resourceNodeSO.maxHealth;
    }

    // ----------------------------------------------------------
    // IDamageable
    // ----------------------------------------------------------

    public virtual void TakeDamage(ToolSO playerTool)
    {
        if (harvestRecipeSO == null) return;
        if (!harvestRecipeSO.IsValidTool(playerTool)) return;

        _currentHealth -= playerTool.power;
        OnHit(playerTool);
        OnHitDrop();

        if (!IsAlive)
            DestroyNode();
    }

    // ----------------------------------------------------------
    // IDroppable
    // ----------------------------------------------------------

    public virtual void DropLoot()
    {
        if (harvestRecipeSO?.dropList == null || harvestRecipeSO.dropList.Count == 0) return;

        foreach (DropEntry entry in harvestRecipeSO.dropList)
        {
            if (UnityEngine.Random.Range(0f, 100f) > entry.dropChance) continue;

            int amount = UnityEngine.Random.Range(entry.minAmount, entry.maxAmount + 1);
            SpawnDrops(entry, amount);
        }
    }

    // ----------------------------------------------------------
    // IInteractable
    // ----------------------------------------------------------

    public virtual void OnIndicatorEnter() { }
    public virtual void OnIndicatorExit()  { }

    // ----------------------------------------------------------
    // Public API — Save/Load
    // ----------------------------------------------------------

    /// <summary>
    /// Directly sets health without triggering damage effects.
    /// Called by SaveManager after scene load to restore damaged state.
    /// </summary>
    public void LoadNodeState(int health)
    {
        _currentHealth = health;
    }

    /// <summary>
    /// Override in subclasses to restore a special secondary state (e.g. FruitTree fruit loss).
    /// Base is a no-op.
    /// </summary>
    public virtual void LoadFruitlessState() { }

    // ----------------------------------------------------------
    // Protected hooks
    // ----------------------------------------------------------

    /// <summary>Called once per hit, before destroy check.</summary>
    protected virtual void OnHit(ToolSO playerTool) { }

    protected virtual void OnHitDrop() { }

    /// <summary>Called when health reaches zero, before the delay timer.</summary>
    protected virtual void OnDestroyed() { }

    protected virtual float DestroyDelay => 0f;

    // ----------------------------------------------------------
    // Private helpers
    // ----------------------------------------------------------

    private void DestroyNode()
    {
        Player.Instance.ClearCurrentTarget();
        DropLoot();
        OnDestroyed();

        if (DestroyDelay > 0f)
            Invoke(nameof(DestroyGameObject), DestroyDelay);
        else
            DestroyGameObject();
    }

    /// <summary>Spawns a drop entry N times with a bounce arc.</summary>
    protected void SpawnDrops(DropEntry entry, int amount)
    {
        if (entry.item?.prefab == null) return;

        Vector3 origin = transform.position;
        for (int i = 0; i < amount; i++)
        {
            Vector3 scatter = new Vector3(
                UnityEngine.Random.Range(-0.8f, 0.8f),
                UnityEngine.Random.Range(-0.8f, 0.8f),
                0f
            );
            Transform spawned = Instantiate(entry.item.prefab, origin, Quaternion.identity);
            if (spawned.TryGetComponent<ItemBounceObject>(out var bounce))
                bounce.StartBounce(origin, origin + scatter);
        }
    }

    private void DestroyGameObject()
    {
        // Notify SaveManager before the GameObject disappears
        if (!string.IsNullOrEmpty(nodeID))
            OnNodeDestroyed?.Invoke(nodeID);

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Generates nodeID from the full hierarchy path (e.g. "Forest/Trees/AppleTree (2)").
    /// Path is unique as long as object names are unique — rename objects if there are duplicates.
    /// Run via right-click on the component → "Generate Node ID".
    /// </summary>
    [ContextMenu("Generate Node ID")]
    private void GenerateNodeID()
    {
        nodeID = GetHierarchyPath(transform);
        UnityEditor.EditorUtility.SetDirty(gameObject);
        UnityEngine.Debug.Log($"[ResourceNode] NodeID set: \"{nodeID}\"", gameObject);
    }

    private static string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t    = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
#endif
}
