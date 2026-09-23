using UnityEngine;

public enum NpcTierPlaceholder
{
    Mob,
    Major,
}

// Marks a spot in the scene reserved for a future GossipNet-backed NPC.
// The NpcId matches the id this NPC will be registered under in
// GossipNet's NpcRegistry once the package is wired in (OVERVIEW.md §6.1).
public class NpcPlaceholder : MonoBehaviour
{
    [SerializeField] private string _npcId;
    [SerializeField] private NpcTierPlaceholder _tier;

    // NPCは静止配置で回転しないため、向きは動的計算せずInspectorで固定指定する
    // (TownActionTriggersのJEV視野角判定が使う)。
    [SerializeField] private Vector2 _facingDirection = Vector2.down;

    public string NpcId => _npcId;
    public NpcTierPlaceholder Tier => _tier;
    public Vector2 FacingDirection => _facingDirection;
}
