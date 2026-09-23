using UnityEngine;

// A physical, breakable pot standing in for GossipNet's "壺をこわした" context
// trigger (OVERVIEW.md §14.1/§7.2), replacing the earlier UI-button
// placeholder with an actual world object the player bumps into. Breaking it
// dims the sprite, disables its collider, and calls
// TownActionTriggers.BreakPot(transform.position) (OVERVIEW.md §15.5/§16),
// which records the context event, kicks off a manual cache refresh, and
// runs JEV for nearby major NPCs.
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class BreakablePot : MonoBehaviour
{
    [SerializeField] private Color _brokenColor = new Color(0.6f, 0.6f, 0.6f, 0.35f);

    private SpriteRenderer _spriteRenderer;
    private Collider2D _collider;
    private TownActionTriggers _triggers;
    private Color _originalColor;
    private bool _broken;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        _triggers = FindFirstObjectByType<TownActionTriggers>();
        _originalColor = _spriteRenderer.color;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_broken || !other.CompareTag("Player"))
            return;

        Break();
    }

    private void Break()
    {
        _broken = true;
        _collider.enabled = false;
        _spriteRenderer.color = _brokenColor;

        if (_triggers != null)
            _triggers.BreakPot(transform.position);
    }

    // デモの「全部リセット」(DebugViewPlaceholder._resetAllKey)から呼ばれ、再びOnTriggerEnter2Dで
    // 発火できる状態に戻す。
    public void ResetTrigger()
    {
        _broken = false;
        _collider.enabled = true;
        _spriteRenderer.color = _originalColor;
    }
}
