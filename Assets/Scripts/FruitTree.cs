using System.Collections.Generic;
using UnityEngine;

public class FruitTree : Tree
{
    [Header("Fruit Drop On Hit")]
    [SerializeField] private HarvestRecipeSO fallRecipeSO;

    [Tooltip("Danh sách quả rơi ra khi bị kích hoạt. " +
             "Drop Chance trong từng entry KHÔNG có hiệu quả ở đây — " +
             "toàn bộ quả trong list rơi cùng lúc khi event được trigger.")]
    [SerializeField] private List<DropEntry> fruitDropOnHit;

    [Tooltip("Xác suất (%) mỗi lần bị chặt sẽ kích hoạt rơi toàn bộ quả. " +
             "VD: 40 = 40% mỗi nhát rìu có thể làm quả rơi hết.")]
    [SerializeField][Range(0f, 100f)] private float fruitDropTriggerChance = 40f;

    [Header("Fruit Sprites")]
    [SerializeField] private Animator fruitTreeAnimator;

    [Header("Save/Load")]
    [Tooltip("Tên Animator state hiển thị cây không có trái (dùng khi load). " +
             "Phải khớp chính xác tên state trong Animator Controller.")]
    [SerializeField] private string noFruitIdleStateName = "TimberTree_Idle";

    private bool _hasFruit = true;

    private const string PARAM_FRUIT_SHAKE = "FruitShake";
    private const string PARAM_FRUIT_DROP = "FruitDrop";
    private const string PARAM_NO_FRUIT = "NoFruitShake";

    protected override Animator TreeAnimator => fruitTreeAnimator;

    // ----------------------------------------------------------
    // Save/Load overrides
    // ----------------------------------------------------------

    /// <summary>Exposes fruit state for SaveManager to snapshot.</summary>
    public override bool HasFruit => _hasFruit;

    /// <summary>
    /// Restores the fruitless state without playing animations or dropping items.
    /// Called by SaveManager when loading a session where fruit was already lost.
    /// </summary>
    public override void LoadFruitlessState()
    {
        _hasFruit = false;
        SwitchToFallRecipe();

        // Jump directly to the no-fruit idle visual — no drop animation on load.
        if (fruitTreeAnimator != null && !string.IsNullOrEmpty(noFruitIdleStateName))
            fruitTreeAnimator.Play(noFruitIdleStateName, 0, 0f);
        else
            Debug.LogWarning($"[FruitTree] noFruitIdleStateName is empty on '{name}'. " +
                             "Set it in the Inspector to restore the correct sprite on load.");
    }

    protected override void OnHit(ToolSO playerTool)
    {
        PlayHitSound();

        if (!_hasFruit)
        {
            fruitTreeAnimator?.SetTrigger(PARAM_NO_FRUIT);
            return;
        }

        if (Random.Range(0f, 100f) <= fruitDropTriggerChance)
        {
            fruitTreeAnimator?.SetTrigger(PARAM_FRUIT_DROP);
            DropAllFruits();
            _hasFruit = false;
            SwitchToFallRecipe();
        }
        else
        {
            fruitTreeAnimator?.SetTrigger(PARAM_FRUIT_SHAKE);
        }
    }

    protected override void OnDestroyed()
    {
        // Nếu còn quả khi ngã → rơi quả trước
        if (_hasFruit)
        {
            fruitTreeAnimator?.SetTrigger(PARAM_FRUIT_DROP);
            DropAllFruits();
        }

        // Gọi base → SetTrigger FALL + DestroyDelay từ Tree
        base.OnDestroyed();
    }

    private void DropAllFruits()
    {
        if (fruitDropOnHit == null || fruitDropOnHit.Count == 0) return;

        foreach (DropEntry entry in fruitDropOnHit)
        {
            int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
            SpawnDrops(entry, amount);
        }
    }

    private void SwitchToFallRecipe()
    {
        if (fallRecipeSO != null)
            harvestRecipeSO = fallRecipeSO;
    }
}