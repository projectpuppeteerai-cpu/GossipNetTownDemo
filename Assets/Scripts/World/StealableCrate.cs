using UnityEngine;

// A stealable crate of goods - a second "実物オブジェクト" context trigger
// alongside BreakablePot (OVERVIEW.md §14.6 philosophy: represent context
// events as world objects the player bumps into, not UI buttons). Reuses
// the pot sprite with a distinct tint since no dedicated crate art has been
// cut from the Kenney sheet yet.
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class StealableCrate : MonoBehaviour
{
    [SerializeField] private Color _takenColor = new Color(0.6f, 0.6f, 0.6f, 0.35f);

    private SpriteRenderer _spriteRenderer;
    private Collider2D _collider;
    private TownActionTriggers _triggers;
    private Color _originalColor;
    private bool _taken;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        _triggers = FindFirstObjectByType<TownActionTriggers>();
        _originalColor = _spriteRenderer.color;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_taken || !other.CompareTag("Player"))
            return;

        Take();
    }

    private void Take()
    {
        _taken = true;
        _collider.enabled = false;
        _spriteRenderer.color = _takenColor;

        if (_triggers != null)
            _triggers.StealItem(transform.position);
    }

    // デモの「全部リセット」(DebugViewPlaceholder._resetAllKey)から呼ばれ、再びOnTriggerEnter2Dで
    // 発火できる状態に戻す。
    public void ResetTrigger()
    {
        _taken = false;
        _collider.enabled = true;
        _spriteRenderer.color = _originalColor;
    }
}
