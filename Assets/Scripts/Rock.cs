// ──────────────────────────────────────────────
// TheSprouty | Scripts/Rock.cs
// Abstract base for all rock-type ResourceNodes.
// Plays hit particles on each hit. Subclasses add their own behaviour.
// ──────────────────────────────────────────────
using UnityEngine;

public abstract class Rock : ResourceNode
{
    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    //[Header("Rock FX")]
    //[SerializeField] private ParticleSystem hitParticles;

    [Header("Sound")]
    [SerializeField] private SoundEventSO hitSound;

    // ----------------------------------------------------------
    // Protected hooks
    // ----------------------------------------------------------

    /// <summary>Plays hit sound and particles. Subclasses call base.OnHit() to keep this behaviour.</summary>
    protected override void OnHit(ToolSO playerTool)
    {
        hitSound?.Play();
        //hitParticles?.Play();
    }
}
