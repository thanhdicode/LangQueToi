// ──────────────────────────────────────────────
// TheSprouty | Scripts/Environment/House.cs
// Trigger zone that switches player footstep sound
// between grass (outdoor) and wood (indoor).
// Attach on an invisible trigger collider covering the house interior.
// ──────────────────────────────────────────────
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class House : MonoBehaviour
{
    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // ----------------------------------------------------------
    // Private methods
    // ----------------------------------------------------------

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerAnimator animator = other.GetComponentInChildren<PlayerAnimator>();
        animator?.SetIndoor(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerAnimator animator = other.GetComponentInChildren<PlayerAnimator>();
        animator?.SetIndoor(false);
    }
}
