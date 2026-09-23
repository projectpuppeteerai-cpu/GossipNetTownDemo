using UnityEngine;

// A pickup that adds an inventory tag (default "鋼装備一式") to the player's
// context via ContextManager.AddInventoryTag when touched - exercises the
// InventoryTags API added alongside WorldFlags/RecordAction/JEV context
// setters. Routes through TownActionTriggers.PickUpEquipment so NPCs also
// see a "鋼の装備一式を手に入れた" context event and nearby majors can
// perceive it via JEV (a guard noticing you're suddenly armed is a
// meaningful JEV case, unlike a plain silent inventory change).
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class EquipmentPickup : MonoBehaviour
{
    [SerializeField] private string _equipmentTag = "鋼装備一式";

    private SpriteRenderer _spriteRenderer;
    private Collider2D _collider;
    private TownActionTriggers _triggers;
    private bool _taken;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        _triggers = FindFirstObjectByType<TownActionTriggers>();
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
        _spriteRenderer.enabled = false; // 拾われて無くなった見た目

        if (_triggers != null)
            _triggers.PickUpEquipment(_equipmentTag, transform.position);
    }

    // デモの「全部リセット」(DebugViewPlaceholder._resetAllKey)から呼ばれ、再び拾える状態に戻す。
    // 所持品タグ自体の削除はContextManager.ResetHistory側（呼び出し元）が一括で行う。
    public void ResetTrigger()
    {
        _taken = false;
        _collider.enabled = true;
        _spriteRenderer.enabled = true;
    }
}
