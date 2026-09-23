using UnityEngine;

// A marked-off restricted zone (semi-transparent ground overlay + trigger
// collider, OVERVIEW.md §16 additional event triggers) the player isn't
// supposed to wander into. Fires every time the player enters - no cooldown
// or one-shot state, mirroring BreakablePot/StealableCrate's simplicity;
// repeated entries just mean repeated context/JEV events, same as breaking
// multiple pots would.
[RequireComponent(typeof(Collider2D))]
public class RestrictedZoneTrigger : MonoBehaviour
{
    private TownActionTriggers _triggers;

    private void Awake()
    {
        _triggers = FindFirstObjectByType<TownActionTriggers>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (_triggers != null)
            _triggers.TrespassRestrictedArea(transform.position);
    }
}
