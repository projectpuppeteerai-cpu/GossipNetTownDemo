using UnityEngine;
using UnityEngine.InputSystem;

// A distressed villager the player can help via a dedicated key while in
// range - a positive-valence context event, unlike the pot/crate's
// auto-trigger-on-touch (OVERVIEW.md §16 additional event triggers; helping
// reads better as a deliberate choice than a collision side effect). Reuses
// a mob character sprite with a distinct tint since no dedicated art has
// been cut for this NPC.
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class HelpableVillager : MonoBehaviour
{
    [SerializeField] private Key _helpKey = Key.H;
    [SerializeField] private string _displayName = "困っている住民";
    [SerializeField] private Color _helpedColor = new Color(0.6f, 0.85f, 1f);

    private SpriteRenderer _spriteRenderer;
    private TownActionTriggers _triggers;
    private Color _originalColor;
    private bool _playerInRange;
    private bool _helped;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _triggers = FindFirstObjectByType<TownActionTriggers>();
        _originalColor = _spriteRenderer.color;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _playerInRange = false;
    }

    private void Update()
    {
        if (_helped || !_playerInRange || Keyboard.current == null)
            return;

        if (Keyboard.current[_helpKey].wasPressedThisFrame)
            Help();
    }

    private void Help()
    {
        _helped = true;
        _spriteRenderer.color = _helpedColor;

        if (_triggers != null)
            _triggers.HelpVillager(_displayName, transform.position);
    }

    // デモの「全部リセット」(DebugViewPlaceholder._resetAllKey)から呼ばれ、再びHキーで
    // 助けられる状態に戻す。
    public void ResetTrigger()
    {
        _helped = false;
        _spriteRenderer.color = _originalColor;
    }
}
