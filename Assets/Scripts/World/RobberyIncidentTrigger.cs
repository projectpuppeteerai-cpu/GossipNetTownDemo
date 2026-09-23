using UnityEngine;

// A robbery-in-progress the player can stumble across - unlike the other
// world-object triggers (pot/crate/equipment/help), this represents an
// event happening independently of the player, that they (and nearby
// majors) merely witness. Added specifically to test JEV composition across
// unrelated events (e.g. does perceiving a robbery, followed later by
// picking up equipment, change the NPC's read on the situation at all).
// Fires once (like BreakablePot/StealableCrate), reuses a character sprite
// with a dark tint since no dedicated "bandit" art has been cut.
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class RobberyIncidentTrigger : MonoBehaviour
{
    private TownActionTriggers _triggers;
    private bool _triggered;

    private void Awake()
    {
        _triggers = FindFirstObjectByType<TownActionTriggers>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered || !other.CompareTag("Player"))
            return;

        _triggered = true;

        if (_triggers != null)
            _triggers.WitnessRobbery(transform.position);
    }

    // デモの「全部リセット」(DebugViewPlaceholder._resetAllKey)から呼ばれ、再び目撃できる状態に戻す。
    public void ResetTrigger()
    {
        _triggered = false;
    }
}
