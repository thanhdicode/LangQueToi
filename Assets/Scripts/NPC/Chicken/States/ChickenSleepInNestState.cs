// ──────────────────────────────────────────────
// TheSprouty | NPC/Chicken/States/ChickenSleepInNestState.cs
// Chicken rests inside the nest for a configurable duration.
// Transitions to JumpOutNest when done.
// (Future: if egg is present → Incubating instead.)
//
// NavMeshAgent is disabled during sleep so other animals don't path-avoid
// the resting chicken and the agent doesn't jitter the transform.
// ──────────────────────────────────────────────
using UnityEngine;

public class ChickenSleepInNestState : BaseAnimalState<ChickenNPC>
{
    private float _timer;

    public ChickenSleepInNestState(ChickenNPC owner) : base(owner) { }

    public override void Enter()
    {
        _timer = Owner.ChickenData.nestSleepDuration;

        if (Owner.Agent != null)
            Owner.Agent.enabled = false;
    }

    public override void Tick()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            // TODO: when egg system is ready, check for egg here
            // and transition to IncubatingState instead.
            Owner.StateMachine.ChangeState(Owner.JumpOutNestState);
        }
    }

    public override void Exit()
    {
        if (Owner.Agent == null) return;

        Owner.Agent.enabled = true;
        // Re-attach agent to the navmesh at the chicken's current world position.
        Owner.Agent.Warp(Owner.transform.position);
    }
}
